using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections;

[PublicAPI]
public class DocumentProjection : ReceiveActor, IWithStash
{
    public static class Commands
    {
        public interface IMessageWithId
        {
            IProjectionIdContext Id { get; }
        }

        public record ProjectEvents(IProjectionIdContext Id, ImmutableList<EventWithPosition> Events) : IMessageWithId;

        public record StopInProcessEvents(IProjectionIdContext Id) : IMessageWithId;
    }
    
    public static class Responses
    {
        public record StopInProcessEventsResponse(Exception? Error = null);
    }

    private readonly ProjectionConfiguration _configuration;
    private readonly ILoggingAdapter _logger;

    public DocumentProjection(ISupplyProjectionConfigurations configSupplier)
    {
        _logger = Context.GetLogger();

        _configuration = configSupplier.GetConfiguration();

        Become(NotLoaded);
    }

    public IStash Stash { get; set; } = null!;

    private void NotLoaded()
    {
        Receive<Commands.ProjectEvents>(cmd =>
        {
            ProjectEvents(cmd.Id, cmd.Events, () => _configuration.Load(cmd.Id));
        });

        Receive<Commands.StopInProcessEvents>(_ =>
        {
            var waitingItems = Stash.ClearStash();

            foreach (var waitingItem in waitingItems)
            {
                waitingItem
                    .Sender
                    .Tell(new Messages.Reject(new Exception("Projection stopped")));
            }
            
            Sender.Tell(new Responses.StopInProcessEventsResponse());
        });
    }

    private void Loaded(IProjectionContext context)
    {
        Receive<Commands.ProjectEvents>(cmd =>
        {
            ProjectEvents(cmd.Id, cmd.Events, () => Task.FromResult(context));
        });

        Receive<Commands.StopInProcessEvents>(_ =>
        {
            var waitingItems = Stash.ClearStash();

            foreach (var waitingItem in waitingItems)
            {
                waitingItem
                    .Sender
                    .Tell(new Messages.Reject(new Exception("Projection stopped")));
            }

            Become(NotLoaded);
            
            Sender.Tell(new Responses.StopInProcessEventsResponse());
        });
    }

    private void ProcessingEvents(IActorRef from, CancellationTokenSource cancellation)
    {
        Receive<Commands.ProjectEvents>(_ => { Stash.Stash(); });

        Receive<ProjectionResponse>(cmd =>
        {
            from.Tell(cmd.Response);

            Stash.UnstashAll();

            if (cmd is { IsSuccess: true, ProjectedContext: not null })
                Become(() => Loaded(cmd.ProjectedContext));
            else
                Become(NotLoaded);
        });

        ReceiveAsync<Commands.StopInProcessEvents>(async _ =>
        {
            await cancellation.CancelAsync();

            var rejectionResponse = new Messages.Reject(new Exception("Projection stopped"));

            from.Tell(rejectionResponse);
            
            var waitingItems = Stash.ClearStash();

            foreach (var waitingItem in waitingItems)
            {
                waitingItem
                    .Sender
                    .Tell(rejectionResponse);
            }

            Become(NotLoaded);
            
            Sender.Tell(new Responses.StopInProcessEventsResponse());
        });
    }

    private void ProjectEvents(
        IProjectionIdContext id,
        ImmutableList<EventWithPosition> events,
        Func<Task<IProjectionContext>> loadContext)
    {
        var cancellation = new CancellationTokenSource();

        StartProjectingEvents(
                id,
                loadContext,
                events,
                cancellation.Token)
            .PipeTo(Self);

        Become(() => ProcessingEvents(Sender, cancellation));
    }

    private async Task<ProjectionResponse> StartProjectingEvents(
        IProjectionIdContext id,
        Func<Task<IProjectionContext>> loadContext,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken)
    {
        IProjectionContext? context;

        try
        {
            context = await loadContext();
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed loading context for {0}.{1}", _configuration.Name, id);

            return new ProjectionResponse(null, new Messages.Reject(e));
        }

        try
        {
            var (projectedContext, result) = await RunProjections();

            await Task.WhenAll(events
                // ReSharper disable once SuspiciousTypeConversion.Global
                .OfType<IEventWithAck>()
                .Select(x => x.Ack()));

            return new ProjectionResponse(projectedContext, new Messages.Acknowledge(result));
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed handling {0} events for {1}.{2}", events.Count, _configuration.Name, id);
            
            await Task.WhenAll(events
                // ReSharper disable once SuspiciousTypeConversion.Global
                .OfType<IEventWithAck>()
                .Select(x => x.Nack(e)));

            return new ProjectionResponse(null, new Messages.Reject(e));
        }
        
        async Task<(IProjectionContext context, long? position)> RunProjections()
        {
            var existsBefore = context.Exists();

            if (events.IsEmpty)
                return (context, null);

            var wasHandled = false;
            
            foreach (var evnt in events.OrderBy(x => x.Position ?? 0))
            {
                wasHandled = await _configuration.HandleEvent(
                    context,
                    evnt.Event,
                    evnt.Position ?? 0,
                    cancellationToken) || wasHandled;
            }

            if (!wasHandled || !existsBefore && !context.Exists())
                return (context, events.GetHighestEventNumber());
            
            await _configuration
                .Store(new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(_configuration.Name, id)] = context.Freeze()
                }.ToImmutableDictionary(), cancellationToken);

            if (context is IResettableProjectionContext resettable)
                context = resettable.Reset();

            return (context, events.GetHighestEventNumber());
        }
    }

    public static Props Init(ISupplyProjectionConfigurations configSupplier)
    {
        return Props.Create(() => new DocumentProjection(configSupplier));
    }

    private record ProjectionResponse(IProjectionContext? ProjectedContext, Messages.IProjectEventsResponse Response)
    {
        public bool IsSuccess => Response is Messages.Acknowledge && ProjectedContext != null;
    }
}