using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster.Sharding;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ShardedProjectors(ActorSystem actorSystem, ClusterShardingSettings settings, int maxNumberOfShards) 
    : IKeepTrackOfProjectors
{
    private readonly ConcurrentDictionary<string, Task<IActorRef>> _projectors = new();

    public async Task<IProjectorProxy> GetProjector<TId, TDocument>(
        TId id,
        ProjectionConfiguration<TId, TDocument> configuration)
        where TId : notnull where TDocument : notnull
    {
        var projector = await _projectors
            .GetOrAdd(
                configuration.Projection.Name,
                name => ClusterSharding.Get(actorSystem).StartAsync(
                    typeName: $"projection-{name}",
                    entityPropsFactory: documentId => Props.Create(() => new DocumentProjection<TId, TDocument>(
                        name,
                        configuration.Projection.IdFromString(documentId),
                        null)),
                    settings: settings,
                    messageExtractor: new MessageExtractor<TId, TDocument>(maxNumberOfShards, configuration)));

        return new ActorRefProjectorProxy<TId, TDocument>(
            id,
            projector,
            configuration.ProjectionStreamConfiguration.ProjectDocumentTimeout);
    }
    
    private class MessageExtractor<TId, TDocument>(
        int maxNumberOfShards,
        ProjectionConfiguration<TId, TDocument> configuration)
        : HashCodeMessageExtractor(maxNumberOfShards) where TId : notnull where TDocument : notnull
    {
        public override string EntityId(object message)
        {
            return message is DocumentProjection<TId, TDocument>.Commands.IMessageWithId messageWithId 
                ? configuration.Projection.IdToString(messageWithId.Id) 
                : "";
        }
    }
}