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
        public record StartProjecting(
            IHandleEventInProjection<TId, TDocument>.DocumentIdResponse Id,
            IImmutableList<EventWithPosition> Events)
        {
            public long? HighestEventNumber => Events.Select(x => x.Position).Max();
        }

        public record IdFinished(TId Id);
    }

    public static class Responses
    {
        public record StartProjectingResponse(Task<Messages.IProjectEventsResponse> Task);
    }

    private readonly List<TId> _inprogressIds = [];
    private readonly ProjectionConfiguration<TId, TDocument> _configuration;

    private readonly
        Dictionary<TId, Queue<(IImmutableList<EventWithPosition> events, TaskCompletionSource<Messages.IProjectEventsResponse>
            task)>> _queues = new();

    public ProjectionSequencer()
    {
        _configuration = Context.System.GetExtension<ProjectionConfiguration<TId, TDocument>>();

        Receive<Commands.StartProjecting>(cmd =>
        {
            if (!cmd.Id.HasMatch || cmd.Id.Id == null)
            {
                Sender.Tell(new Responses.StartProjectingResponse(
                    Task.FromResult<Messages.IProjectEventsResponse>(new Messages.Acknowledge(cmd.HighestEventNumber))));

                return;
            }
            
            if (!_inprogressIds.Contains(cmd.Id.Id))
            {
                var self = Self;

                var task = Run(cmd.Id.Id, cmd.Events);

                task
                    .ContinueWith(_ => self.Tell(new Commands.IdFinished(cmd.Id.Id)));

                _inprogressIds.Add(cmd.Id.Id);
                
                Sender.Tell(new Responses.StartProjectingResponse(task));
            }
            else
            {
                var promise =
                    new TaskCompletionSource<Messages.IProjectEventsResponse>(TaskCreationOptions
                        .RunContinuationsAsynchronously);

                if (!_queues.TryGetValue(cmd.Id.Id, out var value))
                {
                    value = new Queue<(IImmutableList<EventWithPosition>, TaskCompletionSource<Messages.IProjectEventsResponse>)>();
                    
                    _queues[cmd.Id.Id] = value;
                }

                value.Enqueue((cmd.Events, promise));
                
                Sender.Tell(new Responses.StartProjectingResponse(promise.Task));
            }
        });

        Receive<Commands.IdFinished>(cmd =>
        {
            if (_queues.TryGetValue(cmd.Id, out var value))
            {
                if (value.TryDequeue(out var item))
                {
                    var self = Self;

                    Run(cmd.Id, item.events)
                        .ContinueWith(result =>
                        {
                            if (result.IsCompletedSuccessfully)
                            {
                                item.task.SetResult(result.Result);
                            }
                            else
                            {
                                item.task.TrySetException(result.Exception ??
                                                          new Exception("Failed projecting events"));
                            }
                            
                            self.Tell(new Commands.IdFinished(cmd.Id));
                        });
                    
                    return;
                }

                _queues.Remove(cmd.Id);
            }
            
            if (_inprogressIds.Contains(cmd.Id))
                _inprogressIds.Remove(cmd.Id);
        });
    }

    private async Task<Messages.IProjectEventsResponse> Run(TId id, IImmutableList<EventWithPosition> events)
    {
        var projector = await _configuration.ProjectorFactory.GetProjector(id, _configuration);

        return await projector.ProjectEvents(events);
    }

    public static IActorRef Create(IActorRefFactory refFactory)
    {
        return refFactory.ActorOf(
            Props.Create(() => new ProjectionSequencer<TId, TDocument>()));
    }
}