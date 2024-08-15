using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public class InProcessSingletonProjectionCoordinator(ActorSystem actorSystem) : IStartProjectionCoordinator
{
    public Task<IActorRef> Start<TId, TDocument>(ProjectionConfiguration<TId, TDocument> configuration)
        where TId : notnull where TDocument : notnull
    {
        return Task.FromResult(actorSystem.ActorOf(
            Props.Create(() =>
                new ProjectionsCoordinator<TId, TDocument>(configuration.Projection.Name)),
            configuration.Projection.Name));
    }
}