using System.Collections.Concurrent;
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
    
    internal static void ConfigureProjection(
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