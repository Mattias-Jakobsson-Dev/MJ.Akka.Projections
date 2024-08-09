using Akka.Actor;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public static class InMemoryStorageConfigurationExtensions
{
    public static IProjectionConfigurationSetup<string, TDocument> WithInMemoryStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup) where TDocument : notnull
    {
        return setup.WithProjectionStorage(new InMemoryProjectionStorage());
    }
    
    public static IProjectionConfigurationSetup<string, TDocument> WithBatchedInMemoryStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup,
        ActorSystem actorSystem,
        int batchSize = 100,
        int parallelism = 5) where TDocument : notnull
    {
        return setup.WithProjectionStorage(new InMemoryProjectionStorage().Batched(actorSystem, batchSize, parallelism));
    }
    
    public static IProjectionConfigurationSetup<string, TDocument> WithInMemoryPositionStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup) where TDocument : notnull
    {
        return setup.WithPositionStorage(new InMemoryPositionStorage());
    }
}