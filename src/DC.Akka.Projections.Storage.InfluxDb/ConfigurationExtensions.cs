using DC.Akka.Projections.Configuration;
using InfluxDB.Client;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IConfigurePart<TConfig, InfluxDbProjectionStorage> WithRavenDbDocumentStorage<TConfig>(
        this IHaveConfiguration<TConfig> source,
        IInfluxDBClient client) where TConfig : ProjectionConfig
    {
        return source.WithProjectionStorage( new InfluxDbProjectionStorage(client));
    }
}