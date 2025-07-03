using System.Collections.Concurrent;
using System.Collections.Immutable;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Cluster.Sharding;

internal static class StaticProjectionConfigurations
{
    private static readonly ConcurrentDictionary<string, IImmutableDictionary<string, ProjectionConfiguration>>
        Configurations = new();

    public static void ConfigureRunner(
        string runnerId,
        IImmutableDictionary<string, ProjectionConfiguration> configurations)
    {
        Configurations.AddOrUpdate(
            runnerId,
            _ => configurations,
            (_, _) => configurations);
    }
    
    public static void DisposeRunner(string runnerId)
    {
        Configurations.TryRemove(runnerId, out _);
    }
    
    public static ISupplyProjectionConfigurations SupplierFor(string runner, string projectionName)
    {
        return new Supplier(runner, projectionName);
    }

    public class Supplier(string runnerId, string projectionName) : ISupplyProjectionConfigurations
    {
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
    }
}