using System.Collections.Immutable;
using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public class InProcessSingletonProjectionCoordinator(IImmutableDictionary<string, IProjectionProxy> coordinators) 
    : IProjectionsCoordinator
{
    public IProjectionProxy? Get(string projectionName)
    {
        return coordinators.GetValueOrDefault(projectionName);
    }
    
    public class Setup(ActorSystem actorSystem) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, IProjection> _projections = new();
        
        public void WithProjection(IProjection projection)
        {
            _projections[projection.Name] = projection;
        }

        public Task<IProjectionsCoordinator> Start()
        {
            var result = new Dictionary<string, IProjectionProxy>();
            
            foreach (var projection in _projections)
            {
                var coordinator = actorSystem.ActorOf(
                    projection.Value.CreateCoordinatorProps());
                
                coordinator.Tell(new ProjectionsCoordinator.Commands.Start());

                result[projection.Value.Name] = new ActorRefProjectionProxy(coordinator, projection.Value);
            }

            return Task.FromResult<IProjectionsCoordinator>(
                new InProcessSingletonProjectionCoordinator(result.ToImmutableDictionary()));
        }
    }
}