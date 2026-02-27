using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections;

[PublicAPI]
public class ProjectionSequencer : ReceiveActor
{
    public static class Commands
    {
        public record StartProjecting(IImmutableList<EventWithPosition> Events);

        public record WaitForGroupToFinish(Guid GroupId, PositionData PositionData);
    }
    
    private static class InternalCommands
    {
        public record TaskFinished(
            Guid GroupId,
            Guid TaskId,
            IProjectionIdContext? Id,
            Messages.IProjectEventsResponse Response);
        
        public record Reset(CancellationToken CancellationToken);

        public record Stop(Guid InstanceId);
    }

    public static class Responses
    {
        public record StartProjectingResponse(
            ImmutableList<(
                Guid groupId,
                Guid taskId,
                IProjectionIdContext? idContext,
                Task<Messages.IProjectEventsResponse> task)> Tasks);

        public record WaitForGroupToFinishResponse(PositionData PositionData);
    }

    private readonly List<IProjectionIdContext> _inprogressIds = [];
    private readonly Dictionary<Guid, WaitingGroup> _inProcessGroups = new();

    private readonly ProjectionConfiguration _configuration;
    private readonly ILoggingAdapter _logger;

    private IKeepTrackOfProjectors _projectorFactory;

    private readonly
        Dictionary<object, Queue<(ImmutableList<EventWithPosition> events,
            TaskCompletionSource<Messages.IProjectEventsResponse>
            task)>> _queues = new();

    public ProjectionSequencer(ProjectionConfiguration configuration)
    {
        _logger = Context.GetLogger();
        
        _configuration = configuration;
        _projectorFactory = configuration.ProjectorFactory;

        Become(NotStarted);
    }

    private void NotStarted()
    {
        Receive<InternalCommands.Reset>(cmd =>
        {
            var instanceId = Guid.NewGuid();

            var self = Self;

            cmd.CancellationToken.Register(() => self.Tell(new InternalCommands.Stop(instanceId)));

            _projectorFactory = _projectorFactory.Reset();
            
            Become(() => Started(instanceId, cmd.CancellationToken));
        });
    }

    private void Started(Guid instanceId, CancellationToken cancellationToken)
    {
        ReceiveAsync<Commands.StartProjecting>(async cmd =>
        {
            var self = Self;
            
            var tasks = new List<(
                Guid groupId,
                Guid taskId,
                IProjectionIdContext? idContext,
                Task<Messages.IProjectEventsResponse> task)>();

            var eventsWithIds = await Task.WhenAll(cmd
                .Events
                .SelectMany(x =>
                {
                    var transformed = _configuration.TransformEvent(x.Event).ToList();

                    if (x is not IEventWithAck originalAck)
                        return transformed.Select(y => x with { Event = y });

                    switch (transformed.Count)
                    {
                        case 0:
                            _ = originalAck.Ack();
                            return [];
                        case 1:
                            return [x with { Event = transformed[0] }];
                    }

                    var remaining = transformed.Count;
                    var nacked = 0;

                    return transformed.Select(y => new CountdownAckEvent(y, x.Position, Ack, Nack));

                    Task Nack(Exception? exception)
                    {
                        return Interlocked.Exchange(ref nacked, 1) == 0 ? originalAck.Nack(exception) : Task.CompletedTask;
                    }

                    Task Ack()
                    {
                        return Interlocked.Decrement(ref remaining) == 0 ? originalAck.Ack() : Task.CompletedTask;
                    }
                })
                .Select(async x => new
                {
                    Event = x,
                    Id = await _configuration.GetIdContextFor(x.Event),
                    x.Position
                }));

            var groupedEvents = eventsWithIds
                .GroupBy(x => x.Id)
                .Select(x => (
                    Events: x.Select(y => y.Event).ToImmutableList(),
                    Id: x.Key,
                    Position: x.Min(y => y.Position)))
                .OrderBy(x => x.Position)
                .Select(x => new
                {
                    x.Events,
                    x.Id,
                    TaskId = Guid.NewGuid()
                });

            foreach (var chunk in groupedEvents
                         .Chunk(_configuration.ProjectionEventBatchingStrategy.GetParallelism()))
            {
                var groupId = Guid.NewGuid();

                foreach (var groupedEvent in chunk)
                {
                    if (groupedEvent.Id == null)
                    {
                        tasks.Add((
                            groupId,
                            groupedEvent.TaskId,
                            groupedEvent.Id,
                            AckEvents(groupedEvent.Events, cancellationToken)));

                        continue;
                    }

                    if (!_inprogressIds.Contains(groupedEvent.Id))
                    {
                        _inprogressIds.Add(groupedEvent.Id);

                        tasks.Add((
                            groupId, 
                            groupedEvent.TaskId, 
                            groupedEvent.Id, 
                            Run(groupedEvent.Id, groupedEvent.Events, cancellationToken)));
                    }
                    else
                    {
                        var promise = new TaskCompletionSource<Messages.IProjectEventsResponse>(
                            TaskCreationOptions.RunContinuationsAsynchronously);

                        if (!_queues.TryGetValue(groupedEvent.Id, out var queue))
                        {
                            queue = new Queue<(
                                ImmutableList<EventWithPosition>,
                                TaskCompletionSource<Messages.IProjectEventsResponse>)>();

                            _queues[groupedEvent.Id] = queue;
                        }

                        queue.Enqueue((groupedEvent.Events, promise));

                        tasks.Add((groupId, groupedEvent.TaskId, groupedEvent.Id, promise.Task));
                    }
                }

                _inProcessGroups[groupId] = WaitingGroup.NewGroup(
                    tasks.Select(x => x.taskId).ToImmutableList());

                foreach (var task in tasks)
                {
#pragma warning disable CS4014 // This is intentional, we want the continuations to run without awaiting the tasks here
                    task
                        .task
                        .ContinueWith(result =>
#pragma warning restore CS4014
                        {
                            InternalCommands.TaskFinished response;

                            if (result.IsCompletedSuccessfully)
                            {
                                response = new InternalCommands.TaskFinished(
                                    groupId,
                                    task.taskId,
                                    task.idContext,
                                    result.Result);
                            }
                            else
                            {
                                response = new InternalCommands.TaskFinished(
                                    groupId,
                                    task.taskId,
                                    task.idContext,
                                    new Messages.Reject(result.Exception));
                            }

                            self.Tell(response);
                        }, cancellationToken);
                }
            }

            Sender.Tell(new Responses.StartProjectingResponse(tasks.ToImmutableList()));
        });

        Receive<Commands.WaitForGroupToFinish>(cmd =>
        {
            if (!_inProcessGroups.TryGetValue(cmd.GroupId, out var group))
            {
                Sender.Tell(new Responses.WaitForGroupToFinishResponse(cmd.PositionData));
                
                return;
            }

            group.WithWaiter(Sender, cmd.PositionData);
        });
        
        Receive<InternalCommands.TaskFinished>(cmd =>
        {
            HandleWaitingTasks(cmd.Id, cmd.Response, cancellationToken);

            if (!_inProcessGroups.TryGetValue(cmd.GroupId, out var group)) 
                return;
            
            group.FinishTask(cmd.TaskId);

            if (group.AllFinished())
                _inProcessGroups.Remove(cmd.GroupId);
        });

        ReceiveAsync<InternalCommands.Reset>(async cmd =>
        {
            var newInstanceId = Guid.NewGuid();

            var self = Self;

            cmd.CancellationToken.Register(() => self.Tell(new InternalCommands.Stop(newInstanceId)));
            
            await StopInProgressHandlers();

            _projectorFactory = _projectorFactory.Reset();
            
            Become(() => Started(newInstanceId, cmd.CancellationToken));
        });
        
        ReceiveAsync<InternalCommands.Stop>(async cmd =>
        {
            if (instanceId != cmd.InstanceId)
                return;
            
            await StopInProgressHandlers();

            Become(NotStarted);
        });
    }

    private async Task StopInProgressHandlers()
    {
        try
        {
            foreach (var queue in _queues)
            {
                while (queue.Value.TryDequeue(out var item))
                {
                    item.task.TrySetResult(new Messages.Reject(new Exception("Projection was stopped")));
                }
            }
            
            var projectors = await Task.WhenAll(_inprogressIds
                .Select(id => _projectorFactory.GetProjector(id, _configuration)));

            await Task.WhenAll(projectors
                .Select(projector => projector.StopAllInProgress(_configuration.GetProjection().ProjectionTimeout)));
        }
        catch (Exception e)
        {
            _logger
                .Warning(e, "Failed stopping one or more projectors for {0}", _configuration.Name);
        }
        finally
        {
            _inprogressIds.Clear();
            
            _inProcessGroups.Clear();
            
            _queues.Clear();
        }
    }

    private void HandleWaitingTasks(
        IProjectionIdContext? id,
        Messages.IProjectEventsResponse response,
        CancellationToken cancellationToken)
    {
        if (id == null)
            return;
        
        if (_queues.TryGetValue(id, out var value))
        {
            if (response is not Messages.Acknowledge)
            {
                while (value.TryDequeue(out var item))
                {
                    item.task.TrySetResult(response);
                }
            } 
            else if (value.TryDequeue(out var queuedItem))
            {
                Run(id, queuedItem.events, cancellationToken)
                    .ContinueWith(result =>
                    {
                        if (result.IsCompletedSuccessfully)
                        {
                            queuedItem.task.TrySetResult(result.Result);
                        }
                        else
                        {
                            queuedItem.task.TrySetException(result.Exception ??
                                                      new Exception("Failed projecting events"));
                        }
                    }, cancellationToken);

                return;
            }

            _queues.Remove(id);
        }

        _inprogressIds.Remove(id);
    }

    private async Task<Messages.IProjectEventsResponse> Run(
        IProjectionIdContext id,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken)
    {
        var projector = await _projectorFactory.GetProjector(id, _configuration);

        return await projector.ProjectEvents(
            events,
            _configuration.GetProjection().ProjectionTimeout,
            cancellationToken);
    }
    
    private static async Task<Messages.IProjectEventsResponse> AckEvents(
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken)
    {
        var ackableEvents = events
            .OfType<IEventWithAck>()
            .ToImmutableList();

        if (ackableEvents.IsEmpty)
            return new Messages.Acknowledge(events.GetHighestEventNumber());

        try
        {
            await Task.WhenAll(ackableEvents.Select(x => x.Ack()));
            
            return new Messages.Acknowledge(events.GetHighestEventNumber());
        }
        catch (Exception e)
        {
            return new Messages.Reject(e);
        }
    }

    public static Proxy Create(
        IActorRefFactory refFactory,
        ProjectionConfiguration configuration)
    {
        var sequencer = refFactory.ActorOf(
            Props.Create(() => new ProjectionSequencer(configuration)));

        return new Proxy(sequencer);
    }
    
    public class Proxy(IActorRef sequencer)
    {
        public IActorRef Ref { get; } = sequencer;

        public void Reset(CancellationToken cancellationToken)
        {
            Ref.Tell(new InternalCommands.Reset(cancellationToken));
        }
    }
    
    private class WaitingGroup
    {
        private readonly List<Guid> _waitingTasks;
        private readonly List<(IActorRef waiter, PositionData positionData)> _waiters = [];

        private WaitingGroup(IImmutableList<Guid> tasks)
        {
            _waitingTasks = tasks.ToList();
        }
        
        public static WaitingGroup NewGroup(ImmutableList<Guid> tasks)
        {
            return new WaitingGroup(tasks);
        }

        public void FinishTask(Guid taskId)
        {
            _waitingTasks.Remove(taskId);
            
            if (!AllFinished()) 
                return;

            foreach (var waiter in _waiters)
            {
                waiter.waiter.Tell(new Responses.WaitForGroupToFinishResponse(waiter.positionData));
            }
        }

        public void WithWaiter(IActorRef waiter, PositionData positionData)
        {
            _waiters.Add((waiter, positionData));
        }

        public bool AllFinished()
        {
            return _waitingTasks.Count == 0;
        }
    }

    private record CountdownAckEvent(object Event, long? Position, Func<Task> AckFunc, Func<Exception?, Task> NackFunc)
        : EventWithPosition(Event, Position), IEventWithAck
    {
        public CountdownAckEvent(EventWithPosition inner, IEventWithAck original)
            : this(inner.Event, inner.Position, original.Ack, original.Nack) { }

        public CountdownAckEvent(EventWithPosition inner, Func<Task> ack, Func<Exception?, Task> nack)
            : this(inner.Event, inner.Position, ack, nack) { }

        public Task Ack() => AckFunc();
        public Task Nack(Exception? exception = null) => NackFunc(exception);
    }
}