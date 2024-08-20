using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonProjectionCoordinator(
    IImmutableDictionary<string, IProjectionProxy> projections) : IProjectionsCoordinator
{
    public Task<IImmutableList<IProjectionProxy>> GetAll()
    {
        return Task.FromResult<IImmutableList<IProjectionProxy>>(projections.Values.ToImmutableList());
    }

    public Task<IProjectionProxy?> Get(string projectionName)
    {
        return Task.FromResult(projections.GetValueOrDefault(projectionName));
    }
    
    public class Setup(
        ActorSystem actorSystem,
        ClusterSingletonManagerSettings settings) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, IProjection> _projections = new();
        
        public void WithProjection(IProjection projection)
        {
            _projections[projection.Name] = projection;
        }

        public Task<IProjectionsCoordinator> Start()
        {
            var projectionProxies = new Dictionary<string, IProjectionProxy>();
            
            foreach (var projection in _projections)
            {
                var coordinator = actorSystem
                    .ActorOf(ClusterSingletonManager.Props(
                            singletonProps: projection.Value.CreateCoordinatorProps(),
                            terminationMessage: new ProjectionsCoordinator.Commands.Kill(),
                            settings: settings),
                        name: projection.Value.Name);
            
                projectionProxies[projection.Value.Name] = new ActorRefProjectionProxy(coordinator, projection.Value);
            }

            return Task.FromResult<IProjectionsCoordinator>(
                new ClusterSingletonProjectionCoordinator(projectionProxies.ToImmutableDictionary()));
        }
    }
}