using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonProjectionCoordinator(
    IImmutableDictionary<string, IProjectionProxy> projections) : IProjectionsCoordinator
{
    public IImmutableList<IProjectionProxy> GetAll()
    {
        return projections.Values.ToImmutableList();
    }

    public IProjectionProxy? Get(string projectionName)
    {
        return projections.GetValueOrDefault(projectionName);
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
                var coordinatorName = $"{projection.Value.Name}-coordinator";

                var coordinatorSettings = settings
                    .WithSingletonName(coordinatorName);
                
                actorSystem
                    .ActorOf(ClusterSingletonManager.Props(
                            projection.Value.CreateCoordinatorProps(),
                            new ProjectionsCoordinator.Commands.Kill(),
                            coordinatorSettings),
                        coordinatorName);

                var coordinator = actorSystem
                    .ActorOf(ClusterSingletonProxy.Props(
                            $"/user/{coordinatorName}",
                            ClusterSingletonProxySettings
                                .Create(actorSystem)
                                .WithRole(coordinatorSettings.Role)
                                .WithSingletonName(coordinatorSettings.SingletonName)),
                        $"{coordinatorName}-proxy");
                
                projectionProxies[projection.Value.Name] = new ActorRefProjectionProxy(coordinator, projection.Value);
            }

            return Task.FromResult<IProjectionsCoordinator>(
                new ClusterSingletonProjectionCoordinator(projectionProxies.ToImmutableDictionary()));
        }
    }
}