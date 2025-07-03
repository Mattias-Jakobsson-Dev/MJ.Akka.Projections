using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster.Sharding;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Cluster.Sharding;

internal class ShardedProjectionConfigurationSupplier(string runnerId, string projectionName) : ISupplyProjectionConfigurations
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ProjectionConfiguration>>
        Configurations = new();
    
    public ProjectionConfiguration GetConfiguration()
    {
        var runnerConfigurations = Configurations
            .TryGetValue(runnerId, out var configurations)
            ? configurations
            : throw new NoDocumentProjectionException(projectionName);

        return runnerConfigurations.TryGetValue(projectionName, out var projectionConfig)
            ? projectionConfig
            : throw new NoDocumentProjectionException(projectionName);
    }
    
    public static void DisposeRunner(string runnerId)
    {
        Configurations.TryRemove(runnerId, out _);
    }
    
    public static void ConfigureProjection(
        string runnerId,
        string projectionName,
        ProjectionConfiguration configuration)
    {
        var runnerConfigurations = Configurations
            .GetOrAdd(runnerId, _ => new ConcurrentDictionary<string, ProjectionConfiguration>());
        
        runnerConfigurations.AddOrUpdate(
            projectionName,
            _ => configuration,
            (_, _) => configuration);
    }
}

public class ShardedProjectors(
    ActorSystem actorSystem,
    ClusterShardingSettings settings,
    int maxNumberOfShards,
    string runnerId) 
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
                    $"projection-{name}",
                    documentId =>
                    {
                        ShardedProjectionConfigurationSupplier
                            .ConfigureProjection(
                                runnerId,
                                name,
                                configuration);
                        
                        return configuration
                            .GetProjection()
                            .CreateProjectionProps(
                                configuration.IdFromString(documentId),
                                new ShardedProjectionConfigurationSupplier(runnerId, name));
                    },
                    settings,
                    new MessageExtractor<TId, TDocument>(maxNumberOfShards, configuration)));

        return new ActorRefProjectorProxy<TId, TDocument>(id, projector);
    }

    public IKeepTrackOfProjectors Reset()
    {
        return this;
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