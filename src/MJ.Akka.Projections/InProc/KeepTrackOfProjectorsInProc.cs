using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Util;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.InProc;

public class KeepTrackOfProjectorsInProc(ActorSystem actorSystem, IHandleProjectionPassivation passivationHandler)
    : IKeepTrackOfProjectors
{
    private readonly ConcurrentDictionary<string, IActorRef> _coordinators = new();

    public Task<IProjectorProxy> GetProjector<TId, TDocument>(TId id, ProjectionConfiguration configuration)
        where TId : notnull where TDocument : notnull
    {
        var coordinator = _coordinators
            .GetOrAdd(
                configuration.Name,
                _ => actorSystem.ActorOf(Props.Create(() =>
                        new InProcDocumentProjectionCoordinator<TId, TDocument>(passivationHandler, configuration)),
                    $"in-proc-projector-{configuration.Name}"));

        return Task.FromResult<IProjectorProxy>(new InProcDocumentProjectorProxy<TId, TDocument>(id, coordinator));
    }

    public IKeepTrackOfProjectors Reset()
    {
        foreach (var coordinator in _coordinators)
            actorSystem.Stop(coordinator.Value);

        return new KeepTrackOfProjectorsInProc(actorSystem, passivationHandler);
    }

    private class InProcDocumentProjectorProxy<TId, TDocument>(TId id, IActorRef coordinator) : IProjectorProxy
        where TId : notnull where TDocument : notnull
    {
        public Task<Messages.IProjectEventsResponse> ProjectEvents(
            ImmutableList<EventWithPosition> events,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return coordinator
                .Ask<Messages.IProjectEventsResponse>(new InProcDocumentProjectionCoordinator<TId, TDocument>.Commands.Project(
                        id,
                        events,
                        timeout), 
                    timeout, 
                    cancellationToken);
        }

        public async Task StopAllInProgress(TimeSpan timeout)
        {
            var response = await coordinator.Ask<DocumentProjection<TId, TDocument>.Responses.StopInProcessEventsResponse>(
                    new InProcDocumentProjectionCoordinator<TId, TDocument>.Commands.StopInProcessEvents(id),
                    timeout);

            if (response.Error is not null)
                throw response.Error;
        }
    }

    private class InProcDocumentProjectionCoordinator<TId, TDocument> : ReceiveActor
        where TId : notnull where TDocument : notnull
    {
        public static class Commands
        {
            public record Project(
                TId Id,
                ImmutableList<EventWithPosition> Events,
                TimeSpan Timeout);
            
            public record StopInProcessEvents(TId Id);
        }

        private static class InternalCommands
        {
            public record RemoveChild(string ChildId);

            public record FinishedProjectingToChild(Guid ProjectionId);
        }

        private readonly ProjectionConfiguration _projectionConfiguration;
        
        private readonly Dictionary<Guid, string> _inProcessHandler = new();
        private readonly List<string> _waitingToBeRemoved = [];

        public InProcDocumentProjectionCoordinator(
            IHandleProjectionPassivation passivationHandler,
            ProjectionConfiguration projectionConfiguration)
        {
            _projectionConfiguration = projectionConfiguration;
            
            var handlePassivation = passivationHandler.StartNew();

            Receive<Commands.Project>(cmd =>
            {
                var id = GetProjectionId(cmd.Id);

                var projectionId = Guid.NewGuid();

                _inProcessHandler[projectionId] = id;

                var projectionRef = Context
                    .Child(id)
                    .GetOrElse(() => Context.ActorOf(
                        projectionConfiguration.GetProjection()
                            .CreateProjectionProps(
                                cmd.Id,
                                new InProcessProjectionConfigurationsSupplier(projectionConfiguration)),
                        id));

                var self = Self;
                var sender = Sender;

                projectionRef
                    .Ask<Messages.IProjectEventsResponse>(
                        new DocumentProjection<TId, TDocument>.Commands.ProjectEvents(
                            cmd.Id,
                            cmd.Events),
                        cmd.Timeout)
                    .ContinueWith(response =>
                    {
                        self.Tell(new InternalCommands.FinishedProjectingToChild(projectionId));

                        sender.Tell(response.IsCompletedSuccessfully
                            ? response.Result
                            : new Messages.Reject(
                                response.Exception ?? new Exception("Failure while projecting events")));
                    });

                handlePassivation.SetAndMaybeRemove(
                    id,
                    toRemove => { self.Tell(new InternalCommands.RemoveChild(toRemove)); });
            });

            Receive<InternalCommands.RemoveChild>(cmd =>
            {
                if (_inProcessHandler.Any(x => x.Value == cmd.ChildId))
                {
                    if (!_waitingToBeRemoved.Contains(cmd.ChildId))
                        _waitingToBeRemoved.Add(cmd.ChildId);
                }
                else
                {
                    var childToRemove = Context.Child(cmd.ChildId);

                    if (!childToRemove.IsNobody())
                        Context.Stop(childToRemove);

                    if (_waitingToBeRemoved.Contains(cmd.ChildId))
                        _waitingToBeRemoved.Remove(cmd.ChildId);
                }
            });

            Receive<InternalCommands.FinishedProjectingToChild>(cmd =>
            {
                _inProcessHandler.Remove(cmd.ProjectionId);

                var childrenToRemove = _waitingToBeRemoved
                    .Where(x => _inProcessHandler.All(y => y.Value != x))
                    .ToImmutableList();

                foreach (var childId in childrenToRemove)
                {
                    var childToRemove = Context.Child(childId);

                    if (!childToRemove.IsNobody())
                        Context.Stop(childToRemove);

                    _waitingToBeRemoved.Remove(childId);
                }
            });

            Receive<Commands.StopInProcessEvents>(cmd =>
            {
                var id = GetProjectionId(cmd.Id);
                
                var projectionRef = Context.Child(id);

                if (!projectionRef.IsNobody())
                    projectionRef.Tell(new DocumentProjection<TId, TDocument>.Commands.StopInProcessEvents(cmd.Id), Sender);
                else
                    Sender.Tell(new DocumentProjection<TId, TDocument>.Responses.StopInProcessEventsResponse());
            });
        }
        
        private string GetProjectionId(TId id)
        {
            return MurmurHash.StringHash(_projectionConfiguration.IdToString(id)).ToString();
        }
    }
}