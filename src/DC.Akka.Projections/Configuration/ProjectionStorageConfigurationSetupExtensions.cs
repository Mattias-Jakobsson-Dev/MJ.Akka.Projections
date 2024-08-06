using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public static class ProjectionStorageConfigurationSetupExtensions
{
    public static IProjectionStorageConfigurationSetup<TId, TDocument> Batched<TId, TDocument>(
        this IProjectionStorageConfigurationSetup<TId, TDocument>  setup,
        (int Number, TimeSpan Timeout)? batching = null,
        int parallelism = 10) where TId : notnull where TDocument : notnull
    {
        return setup.WithStorageSession(new BatchedStorageSession(
            setup.Application,
            batching ?? (100, TimeSpan.FromSeconds(5)),
            parallelism));
    }
    
    public static IProjectionStorageConfigurationSetup<TId, TDocument> Batched<TId, TDocument>(
        this IProjectionStorageConfigurationSetup<TId, TDocument>  setup,
        BatchedStorageSession storageSession) where TId : notnull where TDocument : notnull
    {
        return setup.WithStorageSession(storageSession);
    }
}