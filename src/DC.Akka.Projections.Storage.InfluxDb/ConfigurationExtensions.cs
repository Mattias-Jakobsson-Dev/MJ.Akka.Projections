using DC.Akka.Projections.Configuration;
using InfluxDB.Client;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionStoragePartSetup<T> WithInfluxDbStorage<T>(
        this IProjectionPartSetup<T> setup,
        IInfluxDBClient client)
        where T : IProjectionPartSetup<T>
    {
        var storage = new InfluxDbProjectionStorage(client);

        return setup.WithProjectionStorage(storage);
    }
}