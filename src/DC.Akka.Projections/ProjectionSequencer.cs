using System.Collections.Immutable;
using Akka.Actor;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public class ProjectionSequencer<TId, TDocument> : ReceiveActor
    where TId : notnull where TDocument : notnull
{
    public static class Commands
    {
        public record StartProjecting(IImmutableList<EventWithPosition> Events);

        public record TaskFinished(Guid GroupId, Guid TaskId, TId? Id, Messages.IProjectEventsResponse Response);

        public record WaitForGroupToFinish(Guid GroupId, PositionData PositionData);

        public record StopAllInProgressHandlers;
    }

    public static class Responses
    {
        public record StartProjectingResponse(
            ImmutableList<(
                Guid groupId,
                Guid taskId,
                TId? documentId,
                Task<Messages.IProjectEventsResponse> task)> Tasks);

        public record WaitForGroupToFinishResponse(PositionData PositionData);
    }

    private readonly List<TId> _inprogressIds = [];
    private readonly Dictionary<Guid, WaitingGroup> _inProcessGroups = new();

    private readonly ProjectionConfiguration _configuration;

    private readonly
        Dictionary<TId, Queue<(ImmutableList<EventWithPosition> events,
            TaskCompletionSource<Messages.IProjectEventsResponse>
            task)>> _queues = new();

    public ProjectionSequencer(ProjectionConfiguration configuration, CancellationToken cancellationToken)
    {
        _configuration = configuration;

        var self = Self;

        cancellationToken
            .Register(() => self.Tell(new Commands.StopAllInProgressHandlers()));

        Receive<Commands.StartProjecting>(cmd =>
        {
            var tasks = new List<(
                Guid groupId,
                Guid taskId,
                TId? documentId,
                Task<Messages.IProjectEventsResponse> task)>();

            var groupedEvents = cmd
                .Events
                .SelectMany(x => _configuration
                    .TransformEvent(x.Event)
                    .Select(y => x with
                    {
                        Event = y
                    }))
                .Select(x => new
                {
                    Event = x,
                    Id = _configuration.GetDocumentIdFrom(x.Event),
                    x.Position
                })
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
                         .Chunk(configuration.ProjectionEventBatchingStrategy.GetParallelism()))
            {
                var groupId = Guid.NewGuid();

                foreach (var groupedEvent in chunk)
                {
                    if (!groupedEvent.Id.IsUsable)
                    {
                        tasks.Add((
                            groupId,
                            groupedEvent.TaskId,
                            (TId?)groupedEvent.Id.Id,
                            Task.FromResult<Messages.IProjectEventsResponse>(
                                new Messages.Acknowledge(groupedEvent.Events.GetHighestEventNumber()))));

                        continue;
                    }

                    var id = (TId)groupedEvent.Id.Id;

                    if (!_inprogressIds.Contains(id))
                    {
                        _inprogressIds.Add(id);

                        tasks.Add((groupId, groupedEvent.TaskId, id, Run(id, groupedEvent.Events, cancellationToken)));
                    }
                    else
                    {
                        var promise = new TaskCompletionSource<Messages.IProjectEventsResponse>(
                            TaskCreationOptions.RunContinuationsAsynchronously);

                        if (!_queues.TryGetValue(id, out var queue))
                        {
                            queue = new Queue<(
                                ImmutableList<EventWithPosition>,
                                TaskCompletionSource<Messages.IProjectEventsResponse>)>();

                            _queues[id] = queue;
                        }

                        queue.Enqueue((groupedEvent.Events, promise));

                        tasks.Add((groupId, groupedEvent.TaskId, id, promise.Task));
                    }
                }

                _inProcessGroups[groupId] = WaitingGroup.NewGroup(
                    tasks.Select(x => x.taskId).ToImmutableList());

                foreach (var task in tasks)
                {
                    task
                        .task
                        .ContinueWith(result =>
                        {
                            Commands.TaskFinished response;

                            if (result.IsCompletedSuccessfully)
                            {
                                response = new Commands.TaskFinished(
                                    groupId,
                                    task.taskId,
                                    task.documentId,
                                    result.Result);
                            }
                            else
                            {
                                response = new Commands.TaskFinished(
                                    groupId,
                                    task.taskId,
                                    task.documentId,
                                    new Messages.Reject(result.Exception));
                            }

                            self.Tell(response);
                        });
                }
            }

            Sender.Tell(new Responses.StartProjectingResponse(tasks.ToImmutableList()));
        });

        Receive<Commands.TaskFinished>(cmd =>
        {
            HandleWaitingTasks(cmd.Id, cmd.Response, cancellationToken);

            if (!_inProcessGroups.TryGetValue(cmd.GroupId, out var group)) 
                return;
            
            var waitingGroup = group
                .FinishTask(cmd.TaskId, cmd.Response);

            if (waitingGroup.AllFinished())
            {
                _inProcessGroups.Remove(cmd.GroupId);
            }
            else
            {
                _inProcessGroups[cmd.GroupId] = waitingGroup;
            }
        });

        Receive<Commands.WaitForGroupToFinish>(cmd =>
        {
            if (!_inProcessGroups.TryGetValue(cmd.GroupId, out var group))
            {
                Sender.Tell(new Responses.WaitForGroupToFinishResponse(cmd.PositionData));
                
                return;
            }

            _inProcessGroups[cmd.GroupId] = group.WithWaiter(Sender, cmd.PositionData);
        });

        ReceiveAsync<Commands.StopAllInProgressHandlers>(async _ =>
        {
            foreach (var inprogressId in _inprogressIds)
            {
                var projector = await _configuration
                    .ProjectorFactory
                    .GetProjector<TId, TDocument>(inprogressId, _configuration);

                projector.StopAllInProgress();
            }
        });
    }

    private void HandleWaitingTasks(
        TId? id,
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
                    item.task.SetResult(response);
                }
            } 
            else if (value.TryDequeue(out var queuedItem))
            {
                Run(id, queuedItem.events, cancellationToken)
                    .ContinueWith(result =>
                    {
                        if (result.IsCompletedSuccessfully)
                        {
                            queuedItem.task.SetResult(result.Result);
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
        TId id,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken)
    {
        var projector = await _configuration.ProjectorFactory.GetProjector<TId, TDocument>(id, _configuration);

        return await projector.ProjectEvents(
            events,
            _configuration.GetProjection().ProjectionTimeout,
            cancellationToken);
    }

    public static IActorRef Create(
        IActorRefFactory refFactory,
        ProjectionConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return refFactory.ActorOf(
            Props.Create(() => new ProjectionSequencer<TId, TDocument>(configuration, cancellationToken)));
    }
    
    private record WaitingGroup(
        ImmutableList<Guid> WaitingTasks,
        ImmutableDictionary<Guid, Messages.IProjectEventsResponse> FinishedTasks,
        ImmutableList<(IActorRef waiter, PositionData positionData)> Waiters)
    {
        public static WaitingGroup NewGroup(ImmutableList<Guid> tasks)
        {
            return new WaitingGroup(
                tasks,
                ImmutableDictionary<Guid, Messages.IProjectEventsResponse>.Empty,
                ImmutableList<(IActorRef, PositionData)>.Empty);
        }

        public WaitingGroup FinishTask(Guid taskId, Messages.IProjectEventsResponse response)
        {
            var result = this with
            {
                WaitingTasks = WaitingTasks.Remove(taskId),
                FinishedTasks = FinishedTasks.SetItem(taskId, response)
            };

            if (!result.AllFinished()) 
                return result;

            foreach (var waiter in result.Waiters)
            {
                waiter.waiter.Tell(new Responses.WaitForGroupToFinishResponse(waiter.positionData));
            }

            return result;
        }

        public WaitingGroup WithWaiter(IActorRef waiter, PositionData positionData)
        {
            return this with
            {
                Waiters = Waiters.Add((waiter, positionData))
            };
        }

        public bool AllFinished()
        {
            return WaitingTasks.IsEmpty;
        }
    }
}