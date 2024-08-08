using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public static class ProjectionStorageConfigurationSetupExtensions
{
    public static IProjectionStorageConfigurationSetup<TId, TDocument> Batched<TId, TDocument>(
        this IProjectionStorageConfigurationSetup<TId, TDocument>  setup,
        int batchSize = 100,
        int parallelism = 10) where TId : notnull where TDocument : notnull
    {
        return setup.WithStorageSession(new BatchedStorageSession(
            setup.Application,
            batchSize,
            parallelism));
    }
    
    public static IProjectionStorageConfigurationSetup<TId, TDocument> Batched<TId, TDocument>(
        this IProjectionStorageConfigurationSetup<TId, TDocument>  setup,
        BatchedStorageSession storageSession) where TId : notnull where TDocument : notnull
    {
        return setup.WithStorageSession(storageSession);
    }
}