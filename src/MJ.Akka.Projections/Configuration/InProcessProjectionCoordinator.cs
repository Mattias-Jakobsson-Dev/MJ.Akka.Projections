using System.Collections.Immutable;
using Akka.Actor;

namespace MJ.Akka.Projections.Configuration;

public class InProcessSingletonProjectionCoordinator(IImmutableDictionary<string, IProjectionProxy> coordinators)
    : IProjectionsCoordinator
{
    public IProjectionProxy? Get(string projectionName)
    {
        return coordinators.GetValueOrDefault(projectionName);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var coordinator in coordinators)
            await coordinator.Value.Stop();
        
        GC.SuppressFinalize(this);
    }

    public class Setup(ActorSystem actorSystem) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, ProjectionConfiguration> _projections = new();

        public void WithProjection(ProjectionConfiguration projection)
        {
            _projections[projection.Name] = projection;
        }

        public Task<IProjectionsCoordinator> Start()
        {
            var result = new Dictionary<string, IProjectionProxy>();

            foreach (var projection in _projections)
            {
                var projectionInstance = projection.Value.GetProjection();

                var coordinator = actorSystem.ActorOf(
                    projectionInstance
                        .CreateCoordinatorProps(new InProcessProjectionConfigurationsSupplier(projection.Value)));

                coordinator.Tell(new ProjectionsCoordinator.Commands.Start());

                result[projection.Value.Name] = new ActorRefProjectionProxy(coordinator, projectionInstance);
            }

            return Task.FromResult<IProjectionsCoordinator>(
                new InProcessSingletonProjectionCoordinator(result.ToImmutableDictionary()));
        }
    }
}