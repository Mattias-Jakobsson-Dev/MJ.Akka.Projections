using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster.Sharding;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ShardedProjectors(ActorSystem actorSystem, ClusterShardingSettings settings, int maxNumberOfShards) 
    : IKeepTrackOfProjectors
{
    private readonly ConcurrentDictionary<string, Task<IActorRef>> _projectors = new();

    public async Task<IProjectorProxy> GetProjector<TId, TDocument>(TId id, ProjectionConfiguration configuration)
        where TId : notnull where TDocument : notnull
    {
        var projector = await _projectors
            .GetOrAdd(
                configuration.Name,
                name => ClusterSharding.Get(actorSystem).StartAsync(
                    typeName: $"projection-{name}",
                    entityPropsFactory: documentId => configuration
                        .GetProjection()
                        .CreateProjectionProps(configuration.IdFromString(documentId)),
                    settings: settings,
                    messageExtractor: new MessageExtractor<TId, TDocument>(maxNumberOfShards, configuration)));

        return new ActorRefProjectorProxy<TId, TDocument>(
            id,
            projector,
            configuration.ProjectionStreamConfiguration.ProjectDocumentTimeout);
    }

    private class MessageExtractor<TId, TDocument>(
        int maxNumberOfShards,
        ProjectionConfiguration configuration)
        : HashCodeMessageExtractor(maxNumberOfShards) where TId : notnull where TDocument : notnull
    {
        public override string EntityId(object message)
        {
            return message is DocumentProjection<TId, TDocument>.Commands.IMessageWithId messageWithId 
                ? configuration.IdToString(messageWithId.Id) 
                : "";
        }
    }
}