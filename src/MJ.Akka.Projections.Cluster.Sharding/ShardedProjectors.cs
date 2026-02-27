using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster.Sharding;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Cluster.Sharding;

public class ShardedProjectors(
    ActorSystem actorSystem,
    ClusterShardingSettings settings,
    int maxNumberOfShards,
    string runnerId) 
    : IKeepTrackOfProjectors
{
    private readonly ConcurrentDictionary<string, Task<IActorRef>> _projectors = new();
    
    public async Task<IProjectorProxy> GetProjector(IProjectionIdContext id, ProjectionConfiguration configuration)
    {
        var projector = await _projectors
            .GetOrAdd(
                configuration.Name,
                name => ClusterSharding.Get(actorSystem).StartAsync(
                    $"projection-{name}",
                    _ =>
                    {
                        ShardedProjectionConfigurationSupplier
                            .ConfigureProjection(
                                runnerId,
                                name,
                                configuration);
                        
                        return configuration
                            .GetProjection()
                            .CreateProjectionProps(
                                new ShardedProjectionConfigurationSupplier(runnerId, name));
                    },
                    settings,
                    new MessageExtractor(maxNumberOfShards)));

        return new ActorRefProjectorProxy(id, projector);
    }

    public IKeepTrackOfProjectors Reset()
    {
        return this;
    }

    private class MessageExtractor(int maxNumberOfShards)
        : HashCodeMessageExtractor(maxNumberOfShards)
    {
        public override string EntityId(object message)
        {
            return message is DocumentProjection.Commands.IMessageWithId messageWithId 
                ? messageWithId.Id.GetProjectorId() 
                : "";
        }
    }
}