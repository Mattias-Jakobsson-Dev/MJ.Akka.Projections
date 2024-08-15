using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonProjectionCoordinator(ActorSystem actorSystem, ClusterSingletonManagerSettings settings)
    : IStartProjectionCoordinator
{
    private readonly Dictionary<string, IProjection> _projections = new();

    private readonly Dictionary<string, IProjectionProxy> _projectionProxies = new();

    public Task IncludeProjection<TId, TDocument>(ProjectionConfiguration<TId, TDocument> configuration) 
        where TId : notnull where TDocument : notnull
    {
        _projections[configuration.Projection.Name] = configuration.Projection;
        
        return Task.CompletedTask;
    }

    public Task Start()
    {
        foreach (var projection in _projections)
        {
            if (_projectionProxies.ContainsKey(projection.Value.Name)) 
                continue;
            
            var coordinator = actorSystem
                .ActorOf(ClusterSingletonManager.Props(
                        singletonProps: projection.Value.CreateCoordinatorProps(),
                        terminationMessage: new ProjectionsCoordinator.Commands.Kill(),
                        settings: settings),
                    name: projection.Value.Name);
            
            _projectionProxies[projection.Value.Name] = new ActorRefProjectionProxy(coordinator);
        }
        
        return Task.CompletedTask;
    }

    public Task<IProjectionProxy?> GetCoordinatorFor(IProjection projection)
    {
        return Task.FromResult(_projectionProxies.GetValueOrDefault(projection.Name));
    }
}