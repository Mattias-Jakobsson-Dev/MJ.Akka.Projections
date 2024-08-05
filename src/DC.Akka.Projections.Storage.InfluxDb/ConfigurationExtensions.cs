using DC.Akka.Projections.Configuration;
using InfluxDB.Client;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionStorageConfigurationSetup<InfluxDbTimeSeriesId, InfluxTimeSeries> WithInfluxDbStorage(
        this IProjectionConfigurationSetup<InfluxDbTimeSeriesId, InfluxTimeSeries> setup,
        IInfluxDBClient client)
    {
        var storage = new InfluxDbProjectionStorage(client);
        
        return setup.WithProjectionStorage(storage);
    }
}