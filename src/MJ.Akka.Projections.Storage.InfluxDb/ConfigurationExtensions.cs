using InfluxDB.Client;
using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IConfigurePart<TConfig, InfluxDbProjectionStorage> WithInfluxDbDocumentStorage<TConfig>(
        this IHaveConfiguration<TConfig> source,
        IInfluxDBClient client) where TConfig : ContinuousProjectionConfig
    {
        return source.WithProjectionStorage(new InfluxDbProjectionStorage(client));
    }
}