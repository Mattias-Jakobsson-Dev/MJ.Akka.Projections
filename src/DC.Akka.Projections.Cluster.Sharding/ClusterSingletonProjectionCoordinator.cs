using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonProjectionCoordinator(ActorSystem actorSystem, ClusterSingletonManagerSettings settings)
    : IStartProjectionCoordinator
{
    public Task<IActorRef> Start<TId, TDocument>(ProjectionConfiguration<TId, TDocument>  configuration)
        where TId : notnull where TDocument : notnull
    {
        var coordinator = actorSystem
            .ActorOf(ClusterSingletonManager.Props(
                    singletonProps: Props.Create(
                        () => new ProjectionsCoordinator<TId, TDocument>(configuration.Projection.Name)),
                    terminationMessage: PoisonPill.Instance,
                    settings: settings),
                name: configuration.Projection.Name);
        
        return Task.FromResult(coordinator);
    }
}