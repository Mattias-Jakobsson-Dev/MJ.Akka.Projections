using Akka.Actor;
using Akka.Util;

namespace DC.Akka.Projections.Configuration;

public class InProcessSingletonProjectionCoordinator(ActorSystem actorSystem) : IStartProjectionCoordinator
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

            var actorName = MurmurHash.StringHash(projection.Value.Name).ToString();
            
            var coordinator = actorSystem.ActorOf(
                projection.Value.CreateCoordinatorProps(),
                actorName);
                
            coordinator.Tell(new ProjectionsCoordinator.Commands.Start());

            _projectionProxies[projection.Value.Name] = new ActorRefProjectionProxy(coordinator);
        }
        
        return Task.CompletedTask;
    }

    public Task<IProjectionProxy?> GetCoordinatorFor(IProjection projection)
    {
        return Task.FromResult(_projectionProxies.GetValueOrDefault(projection.Name));
    }
}