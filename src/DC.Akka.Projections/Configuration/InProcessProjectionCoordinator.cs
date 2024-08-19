using System.Collections.Immutable;
using Akka.Actor;
using Akka.Util;

namespace DC.Akka.Projections.Configuration;

public class InProcessSingletonProjectionCoordinator(IImmutableDictionary<string, IProjectionProxy> coordinators) 
    : IProjectionsCoordinator
{
    public Task<IImmutableList<IProjectionProxy>> GetAll()
    {
        return Task.FromResult<IImmutableList<IProjectionProxy>>(coordinators.Values.ToImmutableList());
    }

    public Task<IProjectionProxy?> Get(string projectionName)
    {
        return Task.FromResult(coordinators.GetValueOrDefault(projectionName));
    }
    
    public class Setup(ActorSystem actorSystem) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, IProjection> _projections = new();
        
        public Task WithProjection(IProjection projection)
        {
            _projections[projection.Name] = projection;
            
            return Task.CompletedTask;
        }

        public Task<IProjectionsCoordinator> Start()
        {
            var result = new Dictionary<string, IProjectionProxy>();
            
            foreach (var projection in _projections)
            {
                var actorName = MurmurHash.StringHash(projection.Value.Name).ToString();
            
                var coordinator = actorSystem.ActorOf(
                    projection.Value.CreateCoordinatorProps(),
                    actorName);
                
                coordinator.Tell(new ProjectionsCoordinator.Commands.Start());

                result[projection.Value.Name] = new ActorRefProjectionProxy(coordinator, projection.Value);
            }

            return Task.FromResult<IProjectionsCoordinator>(
                new InProcessSingletonProjectionCoordinator(result.ToImmutableDictionary()));
        }
    }
}