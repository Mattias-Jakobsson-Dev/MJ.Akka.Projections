using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonProjectionCoordinator(
    IImmutableDictionary<string, IProjectionProxy> projections,
    string runnerId) : IProjectionsCoordinator
{
    public IProjectionProxy? Get(string projectionName)
    {
        return projections.GetValueOrDefault(projectionName);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var coordinator in projections)
            await coordinator.Value.Stop();
        
        StaticProjectionConfigurations.DisposeRunner(runnerId);
        
        GC.SuppressFinalize(this);
    }

    public class Setup(
        ActorSystem actorSystem,
        ClusterSingletonManagerSettings settings) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, ProjectionConfiguration> _projections = new();

        public void WithProjection(ProjectionConfiguration projection)
        {
            _projections[projection.Name] = projection;
        }

        public Task<IProjectionsCoordinator> Start()
        {
            var runnerId = Guid.NewGuid().ToString();
            
            StaticProjectionConfigurations.ConfigureRunner(
                runnerId,
                _projections.ToImmutableDictionary());
            
            var projectionProxies = new Dictionary<string, IProjectionProxy>();

            foreach (var projection in _projections)
            {
                var coordinatorName = $"{projection.Value.Name}-coordinator";

                var coordinatorSettings = settings.WithSingletonName(coordinatorName);

                var projectionInstance = projection.Value.GetProjection();
                
                actorSystem
                    .ActorOf(ClusterSingletonManager.Props(
                            projectionInstance.CreateCoordinatorProps(
                                StaticProjectionConfigurations.SupplierFor(runnerId, projection.Value.Name)),
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
                
                projectionProxies[projection.Value.Name] = new ActorRefProjectionProxy(coordinator, projectionInstance);
            }

            return Task.FromResult<IProjectionsCoordinator>(
                new ClusterSingletonProjectionCoordinator(
                    projectionProxies.ToImmutableDictionary(),
                    runnerId));
        }
    }
}