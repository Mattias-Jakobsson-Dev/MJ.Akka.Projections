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
    
    public async Task<IProjectorProxy> GetProjector(object id, ProjectionConfiguration configuration)
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
                ? messageWithId.Id.ToString() ?? "" 
                : "";
        }
    }
}