using DC.Akka.Projections.Configuration;
using InfluxDB.Client;

namespace DC.Akka.Projections.Storage.InfluxDb;

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