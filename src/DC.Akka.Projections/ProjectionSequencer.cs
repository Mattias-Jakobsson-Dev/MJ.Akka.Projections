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
            DocumentId Id,
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
    
    private readonly ProjectionConfiguration _configuration;

    private readonly
        Dictionary<TId, Queue<(IImmutableList<EventWithPosition> events, TaskCompletionSource<Messages.IProjectEventsResponse>
            task)>> _queues = new();

    public ProjectionSequencer(ProjectionConfiguration configuration)
    {
        _configuration = configuration;
        
        Receive<Commands.StartProjecting>(cmd =>
        {
            if (!cmd.Id.IsUsable)
            {
                Sender.Tell(new Responses.StartProjectingResponse(
                    Task.FromResult<Messages.IProjectEventsResponse>(new Messages.Acknowledge(cmd.HighestEventNumber))));

                return;
            }

            var id = (TId)cmd.Id.Id;
            
            if (!_inprogressIds.Contains(id))
            {
                var self = Self;

                var task = Run(id, cmd.Events);

                task
                    .ContinueWith(_ => self.Tell(new Commands.IdFinished(id)));

                _inprogressIds.Add(id);
                
                Sender.Tell(new Responses.StartProjectingResponse(task));
            }
            else
            {
                var promise =
                    new TaskCompletionSource<Messages.IProjectEventsResponse>(TaskCreationOptions
                        .RunContinuationsAsynchronously);

                if (!_queues.TryGetValue(id, out var value))
                {
                    value = new Queue<(IImmutableList<EventWithPosition>, TaskCompletionSource<Messages.IProjectEventsResponse>)>();
                    
                    _queues[id] = value;
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
        var projector = await _configuration.ProjectorFactory.GetProjector<TId, TDocument>(id, _configuration);

        return await projector.ProjectEvents(events);
    }

    public static IActorRef Create(IActorRefFactory refFactory, ProjectionConfiguration configuration)
    {
        return refFactory.ActorOf(
            Props.Create(() => new ProjectionSequencer<TId, TDocument>(configuration)));
    }
}