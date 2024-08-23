using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public static class ProjectionStorageConfigurationExtensions
{
    public static IConfigurePart<TConfig, BatchedProjectionStorage> Batched<TConfig, TStorage>(
        this IConfigurePart<TConfig, TStorage> source,
        int parallelism = 1,
        IStorageBatchingStrategy? batchingStrategy = null)
        where TConfig : ContinuousProjectionConfig
        where TStorage : IProjectionStorage
    {
        batchingStrategy ??= BatchedProjectionStorage.DefaultStrategy;
        
        return source
            .WithProjectionStorage(source.ItemUnderConfiguration.Batched(
                source.ActorSystem,
                parallelism,
                batchingStrategy));
    }
}