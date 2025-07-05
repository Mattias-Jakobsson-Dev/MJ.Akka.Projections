using Akka.Actor;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Storage.Batched;

public static class BatchedProjectionStorageConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> WithBatchedStorage<TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> config,
        int parallelism = 1,
        IStorageBatchingStrategy? batchingStrategy = null)
    where TStorageSetup : IStorageSetup
    {
        return config.WithModifiedStorage(new Modifier(
            config.ActorSystem,
            parallelism,
            batchingStrategy ?? BatchedProjectionStorage.DefaultStrategy));
    }
    
    private class Modifier(
        ActorSystem actorSystem,
        int parallelism,
        IStorageBatchingStrategy batchingStrategy) : IModifyStorage
    {
        public IStorageSetup Modify(IStorageSetup source)
        {
            return new AddBatchedProjectionStorage(
                source,
                actorSystem,
                parallelism, 
                batchingStrategy);
        }
    }
}