using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.InProc;

public class KeepTrackOfProjectorsInProc : IKeepTrackOfProjectors
{
    private readonly ConcurrentDictionary<string, IActorRef> _coordinators = new();
    private readonly ActorSystem _actorSystem;
    private readonly IHandleProjectionPassivation _passivationHandler;
    private readonly string _instanceId;
    
    public KeepTrackOfProjectorsInProc(
        ActorSystem actorSystem,
        IHandleProjectionPassivation passivationHandler) 
        : this(actorSystem, passivationHandler, Guid.NewGuid().ToString())
    {
        
    }
    
    internal KeepTrackOfProjectorsInProc(
        ActorSystem actorSystem,
        IHandleProjectionPassivation passivationHandler,
        string instanceId)
    {
        _actorSystem = actorSystem;
        _passivationHandler = passivationHandler;
        _instanceId = instanceId;
    }

    public Task<IProjectorProxy> GetProjector(IProjectionIdContext id, ProjectionConfiguration configuration)
    {
        var coordinator = _coordinators
            .GetOrAdd(
                configuration.Name,
                _ => _actorSystem.ActorOf(Props.Create(() =>
                        new InProcDocumentProjectionCoordinator(_passivationHandler, configuration)),
                    $"in-proc-projector-{_instanceId}-{configuration.Name}"));

        return Task.FromResult<IProjectorProxy>(new InProcDocumentProjectorProxy(id, coordinator));
    }

    public IKeepTrackOfProjectors Reset()
    {
        foreach (var coordinator in _coordinators)
            _actorSystem.Stop(coordinator.Value);

        return new KeepTrackOfProjectorsInProc(_actorSystem, _passivationHandler);
    }

    private class InProcDocumentProjectorProxy(IProjectionIdContext id, IActorRef coordinator) : IProjectorProxy
    {
        public Task<Messages.IProjectEventsResponse> ProjectEvents(
            ImmutableList<EventWithPosition> events,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return coordinator
                .Ask<Messages.IProjectEventsResponse>(new InProcDocumentProjectionCoordinator.Commands.Project(
                        id,
                        events,
                        timeout), 
                    timeout, 
                    cancellationToken);
        }

        public async Task StopAllInProgress(TimeSpan timeout)
        {
            var response = await coordinator.Ask<DocumentProjection.Responses.StopInProcessEventsResponse>(
                    new InProcDocumentProjectionCoordinator.Commands.StopInProcessEvents(id),
                    timeout);

            if (response.Error is not null)
                throw response.Error;
        }
    }

    private class InProcDocumentProjectionCoordinator : ReceiveActor
    {
        public static class Commands
        {
            public record Project(
                IProjectionIdContext Id,
                ImmutableList<EventWithPosition> Events,
                TimeSpan Timeout);
            
            public record StopInProcessEvents(IProjectionIdContext Id);
        }

        private static class InternalCommands
        {
            public record RemoveChild(string ChildId);

            public record FinishedProjectingToChild(Guid ProjectionId);
        }

        private readonly Dictionary<Guid, string> _inProcessHandler = new();
        private readonly List<string> _waitingToBeRemoved = [];

        public InProcDocumentProjectionCoordinator(
            IHandleProjectionPassivation passivationHandler,
            ProjectionConfiguration projectionConfiguration)
        {
            var handlePassivation = passivationHandler.StartNew();

            Receive<Commands.Project>(cmd =>
            {
                var id = cmd.Id.GetProjectorId();

                var projectionId = Guid.NewGuid();

                _inProcessHandler[projectionId] = id;

                var projectionRef = Context
                    .Child(id)
                    .GetOrElse(() => Context.ActorOf(
                        projectionConfiguration.GetProjection()
                            .CreateProjectionProps(
                                new InProcessProjectionConfigurationsSupplier(projectionConfiguration)),
                        id));

                var self = Self;
                var sender = Sender;

                projectionRef
                    .Ask<Messages.IProjectEventsResponse>(
                        new DocumentProjection.Commands.ProjectEvents(
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
                var id = cmd.Id.GetProjectorId();
                
                var projectionRef = Context.Child(id);

                if (!projectionRef.IsNobody())
                    projectionRef.Tell(new DocumentProjection.Commands.StopInProcessEvents(cmd.Id), Sender);
                else
                    Sender.Tell(new DocumentProjection.Responses.StopInProcessEventsResponse());
            });
        }
    }
}