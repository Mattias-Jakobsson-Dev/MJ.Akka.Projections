using DC.Akka.Projections.Configuration;
using InfluxDB.Client;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionsSetup WithInfluxDbStorage(
        this IProjectionsSetup setup,
        IInfluxDBClient client)
    {
        var storage = new InfluxDbProjectionStorage(client);
        
        return setup.WithProjectionStorage(storage);
    }
    
    public static IProjectionsSetup WithBatchedInfluxDbStorage(
        this IProjectionsSetup setup,
        IInfluxDBClient client,
        int batchSize = 100,
        int parallelism = 5)
    {
        var storage = new InfluxDbProjectionStorage(client)
            .Batched(
                setup.ActorSystem,
                batchSize, 
                parallelism);
        
        return setup.WithProjectionStorage(storage);
    }
}