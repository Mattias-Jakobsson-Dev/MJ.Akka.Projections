using System.Collections.Immutable;
using System.Diagnostics;
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

        public record ProjectEvents(IProjectionIdContext Id, ImmutableList<EventWithPosition> Events, StashToken? UnstashToken = null) : IMessageWithId;

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
            ProjectEvents(cmd.Id, cmd.Events, () => _configuration.Load(cmd.Id), cmd.UnstashToken);
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
            ProjectEvents(cmd.Id, cmd.Events, () => Task.FromResult(context), cmd.UnstashToken);
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

    private void ProcessingEvents(IProjectionIdContext id, IActorRef from, CancellationTokenSource cancellation)
    {
        Receive<Commands.ProjectEvents>(_ => { Stash.Stash(); });

        Receive<ProjectionResponse>(cmd =>
        {
            from.Tell(cmd.Response);

            // Inject unstashed events as the next batch right after current stashed items
            if (!cmd.UnstashedEvents.IsEmpty)
            {
                Self.Tell(new Commands.ProjectEvents(id, cmd.UnstashedEvents, cmd.UnstashToken));
            }

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
        Func<Task<IProjectionContext>> loadContext,
        StashToken? unstashToken = null)
    {
        var cancellation = new CancellationTokenSource();

        StartProjectingEvents(
                id,
                loadContext,
                events,
                unstashToken,
                cancellation.Token)
            .PipeTo(Self);

        Become(() => ProcessingEvents(id, Sender, cancellation));
    }

    private async Task<ProjectionResponse> StartProjectingEvents(
        IProjectionIdContext id,
        Func<Task<IProjectionContext>> loadContext,
        ImmutableList<EventWithPosition> events,
        StashToken? unstashToken,
        CancellationToken cancellationToken)
    {
        using var activity = ProjectionDiagnostics.ActivitySource
            .StartActivity(
                $"{_configuration.Name}.HandleEvents");

        activity?.SetTag("projection.name", _configuration.Name);
        activity?.SetTag("projection.document.id", id.ToString());
        activity?.SetTag("projection.events.count", events.Count);

        IProjectionContext? context;

        try
        {
            context = await loadContext();
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed loading context for {0}.{1}", _configuration.Name, id);

            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    ["exception.type"] = e.GetType().FullName,
                    ["exception.message"] = e.Message,
                    ["exception.stacktrace"] = e.StackTrace
                }));

            if (unstashToken != null)
                await _configuration.StashStorage.NackUnstash(unstashToken, cancellationToken);

            return new ProjectionResponse(null, new Messages.Reject(e), ImmutableList<EventWithPosition>.Empty, null);
        }
        
        var eventsWithAck = events.OfType<IEventWithAck>().ToImmutableList();
        var sw = Stopwatch.StartNew();

        try
        {
            var (projectedContext, result, unstashedEvents, newUnstashToken) = await RunProjections();

            sw.Stop();
            ProjectionDiagnostics.EventHandlingDuration.Record(
                sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("projection.name", _configuration.Name),
                new KeyValuePair<string, object?>("projection.outcome", "success"));

            if (!eventsWithAck.IsEmpty)
            {
                await Task.WhenAll(eventsWithAck
                    .Select(x => x.Ack(cancellationToken)));
            }

            // Ack the unstash token for the events we just successfully processed
            if (unstashToken != null)
                await _configuration.StashStorage.AckUnstash(unstashToken, cancellationToken);

            ProjectionDiagnostics.EventsProcessed.Add(
                events.Count,
                new KeyValuePair<string, object?>("projection.name", _configuration.Name));

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new ProjectionResponse(projectedContext, new Messages.Acknowledge(result), unstashedEvents,
                newUnstashToken);
        }
        catch (OperationCanceledException e)
        {
            if (!eventsWithAck.IsEmpty)
            {
                await Task.WhenAll(eventsWithAck
                    .Select(x => x.Nack(e, cancellationToken)));
            }

            if (unstashToken != null)
                await _configuration.StashStorage.NackUnstash(unstashToken, cancellationToken);

            return new ProjectionResponse(null, new Messages.Reject(e), ImmutableList<EventWithPosition>.Empty, null);
        }
        catch (Exception e)
        {
            sw.Stop();
            ProjectionDiagnostics.EventHandlingDuration.Record(
                sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("projection.name", _configuration.Name),
                new KeyValuePair<string, object?>("projection.outcome", "failure"));

            _logger.Warning(e, "Failed handling {0} events for {1}.{2}", events.Count, _configuration.Name, id);

            ProjectionDiagnostics.EventsFailed.Add(
                events.Count,
                new KeyValuePair<string, object?>("projection.name", _configuration.Name));

            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    ["exception.type"] = e.GetType().FullName,
                    ["exception.message"] = e.Message,
                    ["exception.stacktrace"] = e.StackTrace
                }));
            
            if (!eventsWithAck.IsEmpty)
            {
                await Task.WhenAll(eventsWithAck
                    .Select(x => x.Nack(e, cancellationToken)));
            }

            if (unstashToken != null)
                await _configuration.StashStorage.NackUnstash(unstashToken, cancellationToken);

            return new ProjectionResponse(null, new Messages.Reject(e), ImmutableList<EventWithPosition>.Empty, null);
        }
        
        async Task<(IProjectionContext context, long? position, ImmutableList<EventWithPosition> unstashedEvents, StashToken? unstashToken)> RunProjections()
        {
            var contextId = new ProjectionContextId(_configuration.Name, id);
            var existsBefore = context.Exists();

            if (events.IsEmpty)
                return (context, null, ImmutableList<EventWithPosition>.Empty, null);

            var wasHandled = false;
            var eventsToStash = ImmutableList<EventWithPosition>.Empty;
            var stashContext = new ProjectionStashContext();
            uint? totalUnstashCount = 0; // 0 means "don't unstash"
            bool unstashAll = false;
            
            foreach (var evnt in events.OrderBy(x => x.Position ?? 0))
            {
                activity?.AddEvent(new ActivityEvent(
                    evnt.Event.GetType().Name,
                    tags: new ActivityTagsCollection
                    {
                        ["event.position"] = evnt.Position
                    }));

                stashContext.Reset();

                wasHandled = await _configuration.HandleEvent(
                    context,
                    evnt.Event,
                    evnt.Position ?? 0,
                    stashContext,
                    cancellationToken) || wasHandled;

                if (stashContext.ShouldStash)
                    eventsToStash = eventsToStash.Add(evnt);

                if (stashContext.ShouldUnstash)
                {
                    if (stashContext.NumberToUnstash == null)
                        unstashAll = true;
                    else if (!unstashAll)
                        totalUnstashCount = (totalUnstashCount ?? 0) + stashContext.NumberToUnstash.Value;
                }
            }

            // Persist stashed events
            if (!eventsToStash.IsEmpty)
            {
                await _configuration.StashStorage.Stash(contextId, eventsToStash, cancellationToken);
            }

            // Peek unstashed events — they are not removed until AckUnstash
            ImmutableList<EventWithPosition> unstashedEvents = ImmutableList<EventWithPosition>.Empty;
            StashToken? newUnstashToken = null;
            if (unstashAll || totalUnstashCount > 0)
            {
                (unstashedEvents, newUnstashToken) = await _configuration.StashStorage.Unstash(
                    contextId,
                    unstashAll ? null : totalUnstashCount,
                    cancellationToken);
            }

            if (!wasHandled || !existsBefore && !context.Exists())
                return (context, events.GetHighestEventNumber(), unstashedEvents, newUnstashToken);
            
            await _configuration
                .Store(new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [contextId] = context.Freeze()
                }.ToImmutableDictionary(), cancellationToken);

            if (context is IResettableProjectionContext resettable)
                context = resettable.Reset();

            return (context, events.GetHighestEventNumber(), unstashedEvents, newUnstashToken);
        }
    }

    public static Props Init(ISupplyProjectionConfigurations configSupplier)
    {
        return Props.Create(() => new DocumentProjection(configSupplier));
    }

    private record ProjectionResponse(
        IProjectionContext? ProjectedContext,
        Messages.IProjectEventsResponse Response,
        ImmutableList<EventWithPosition> UnstashedEvents,
        StashToken? UnstashToken)
    {
        public bool IsSuccess => Response is Messages.Acknowledge && ProjectedContext != null;
    }
}