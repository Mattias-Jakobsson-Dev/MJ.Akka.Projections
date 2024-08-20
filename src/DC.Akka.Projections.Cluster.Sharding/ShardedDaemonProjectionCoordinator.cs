using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ShardedDaemonProjectionCoordinator(
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
        string name,
        ShardedDaemonProcessSettings settings) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, IProjection> _projections = new();
        
        public void WithProjection(IProjection projection)
        {
            _projections[projection.Name] = projection;
        }

        public async Task<IProjectionsCoordinator> Start()
        {
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

            var projectionProxies = new Dictionary<string, IProjectionProxy>();

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

                projectionProxies[sortedProjection.Name] = new ActorRefProjectionProxy(proxy, sortedProjection);
            }

            return new ShardedDaemonProjectionCoordinator(projectionProxies.ToImmutableDictionary());
        }
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