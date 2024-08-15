using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ShardedDaemonProjectionCoordinator(
    ActorSystem actorSystem,
    string name,
    ShardedDaemonProcessSettings settings) : IStartProjectionCoordinator
{
    private readonly Dictionary<string, IProjection> _projections = new();

    private readonly Dictionary<string, IProjectionProxy> _projectionProxies = new();

    private bool _hasStarted;
    
    public Task IncludeProjection<TId, TDocument>(ProjectionConfiguration<TId, TDocument> configuration) 
        where TId : notnull where TDocument : notnull
    {
        _projections[configuration.Projection.Name] = configuration.Projection;
        
        return Task.CompletedTask;
    }

    public Task<IProjectionProxy?> GetCoordinatorFor(IProjection projection)
    {
        return Task.FromResult(_projectionProxies.GetValueOrDefault(projection.Name));
    }

    public async Task Start()
    {
        if (_hasStarted)
            return;
        
        var sortedProjections = _projections
            .Select(x => x.Value)
            .OrderBy(x => x.Name)
            .ToImmutableList();
        
        ShardedDaemonProcess
            .Get(actorSystem)
            .Init(
                name,
                sortedProjections.Count,
                id => sortedProjections[id].CreateCoordinatorProps(),
                settings,
                new ProjectionsCoordinator.Commands.Kill());

        var shardingBaseSettings = settings.ShardingSettings;
        
        if (shardingBaseSettings == null)
        {
            var shardingConfig = actorSystem.Settings.Config.GetConfig("akka.cluster.sharded-daemon-process.sharding");
            var coordinatorSingletonConfig = actorSystem.Settings.Config.GetConfig(shardingConfig.GetString("coordinator-singleton"));
            shardingBaseSettings = ClusterShardingSettings.Create(shardingConfig, coordinatorSingletonConfig);
        }
        
        foreach (var sortedProjection in sortedProjections)
        {
            var proxy = await ClusterSharding
                .Get(actorSystem)
                .StartProxyAsync(
                    $"sharded-daemon-process-{name}",
                    settings.Role ?? shardingBaseSettings.Role,
                    new MessageExtractor(sortedProjections.Count));

            _projectionProxies[sortedProjection.Name] = new ActorRefProjectionProxy(proxy);
        }

        _hasStarted = true;
    }
    
    private sealed class MessageExtractor(int maxNumberOfShards) : HashCodeMessageExtractor(maxNumberOfShards)
    {
        public override string EntityId(object message) => (message as ShardingEnvelope)?.EntityId ?? "";
        
        public override object? EntityMessage(object message) => (message as ShardingEnvelope)?.Message;
        
        public override string ShardId(string entityId, object? messageHint = null)
        {
            return entityId;
        }
    }
}