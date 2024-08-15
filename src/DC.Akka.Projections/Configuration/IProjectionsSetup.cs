using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionsSetup : IProjectionPartSetup<IProjectionsSetup>
{
    IProjectionsSetup WithProjection<TId, TDocument>(
        IProjection<TId, TDocument> projection,
        Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>>? configure = null)
        where TId : notnull where TDocument : notnull;

    Task<ProjectionsApplication> Start();
    
    internal IStartProjectionCoordinator CoordinatorFactory { get; }
    internal IKeepTrackOfProjectors ProjectionFactory { get; }
    internal RestartSettings? RestartSettings { get; }
    internal ProjectionStreamConfiguration ProjectionStreamConfiguration { get; }
    internal IProjectionStorage Storage { get; }
    internal IProjectionPositionStorage PositionStorage { get; }
}

public interface IKeepTrackOfProjectors
{
    Task<IProjectorProxy> GetProjector<TId, TDocument>(
        TId id,
        ProjectionConfiguration<TId, TDocument> configuration)
        where TId : notnull where TDocument : notnull;
}

public interface IProjectorProxy
{
    Task<Messages.IProjectEventsResponse> ProjectEvents(IImmutableList<EventWithPosition> events);
}

public class ActorRefProjectorProxy<TId, TDocument>(TId id, IActorRef projector, TimeSpan timeout) : IProjectorProxy
    where TId : notnull where TDocument : notnull
{
    public Task<Messages.IProjectEventsResponse> ProjectEvents(IImmutableList<EventWithPosition> events)
    {
        return projector.Ask<Messages.IProjectEventsResponse>(
            new DocumentProjection<TId, TDocument>.Commands.ProjectEvents(id, events),
            timeout);
    }
}

public class KeepTrackOfProjectorsInProc(ActorSystem actorSystem) : IKeepTrackOfProjectors
{
    private readonly ConcurrentDictionary<string, IActorRef> _coordinators = new();
    
    public async Task<IProjectorProxy> GetProjector<TId, TDocument>(
        TId id,
        ProjectionConfiguration<TId, TDocument> configuration) 
        where TId : notnull where TDocument : notnull
    {
        var coordinator = _coordinators
            .GetOrAdd(
                configuration.Projection.Name,
                name => actorSystem.ActorOf(Props.Create(() =>
                    new InProcDocumentProjectionCoordinator<TId, TDocument>(name))));
        
        var response = await coordinator
            .Ask<InProcDocumentProjectionCoordinator<TId, TDocument>.Responses.GetProjectionRefResponse>(
                new InProcDocumentProjectionCoordinator<TId, TDocument>.Queries.GetProjectionRef(id));

        return new ActorRefProjectorProxy<TId, TDocument>(
            id,
            response.ProjectionRef,
            configuration.ProjectionStreamConfiguration.ProjectDocumentTimeout);
    }
    
    private class InProcDocumentProjectionCoordinator<TId, TDocument> : ReceiveActor
        where TId : notnull where TDocument : notnull
    {
        public static class Queries
        {
            public record GetProjectionRef(TId Id);
        }

        public static class Responses
        {
            public record GetProjectionRefResponse(IActorRef ProjectionRef);
        }

        public InProcDocumentProjectionCoordinator(string projectionName)
        {
            Receive<Queries.GetProjectionRef>(cmd =>
            {
                var id = SanitizeActorName(cmd.Id.ToString() ?? "");
                
                var projectionRef = Context.Child(id);

                if (projectionRef.IsNobody())
                {
                    projectionRef =
                        Context.ActorOf(
                            Props.Create(
                                () => new DocumentProjection<TId, TDocument>(
                                    projectionName,
                                    cmd.Id,
                                    TimeSpan.FromMinutes(2))),
                            id);
                }

                Sender.Tell(new Responses.GetProjectionRefResponse(projectionRef));
            });
        }

        private static string SanitizeActorName(string id)
        {
            const string validSymbols = "\"-_.*$+:@&=,!~';()";

            if (id.StartsWith('$'))
                id = id[1..];

            var chars = id
                .Where(x => char.IsAsciiLetter(x) || char.IsAsciiDigit(x) || validSymbols.Contains(x));

            return string.Join("", chars);
        }
    }
}