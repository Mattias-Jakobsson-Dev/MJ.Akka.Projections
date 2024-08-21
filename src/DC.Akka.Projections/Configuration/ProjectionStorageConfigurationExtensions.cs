using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public static class ProjectionStorageConfigurationExtensions
{
    public static IConfigurePart<TConfig, BatchedProjectionStorage> Batched<TConfig, TStorage>(
        this IConfigurePart<TConfig, TStorage> source,
        int batchSize = 100,
        int parallelism = 1)
        where TConfig : ContinuousProjectionConfig
        where TStorage : IProjectionStorage
    {
        return source
            .WithProjectionStorage(source.ItemUnderConfiguration.Batched(
                source.ActorSystem,
                batchSize,
                parallelism));
    }
}