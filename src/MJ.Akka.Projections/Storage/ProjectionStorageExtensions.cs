using Akka.Actor;

namespace MJ.Akka.Projections.Storage;

public static class ProjectionStorageExtensions
{
    public static BatchedProjectionStorage Batched(
        this IProjectionStorage storage,
        ActorSystem actorSystem,
        int parallelism,
        IStorageBatchingStrategy batchingStrategy)
    {
        return storage as BatchedProjectionStorage 
               ?? new BatchedProjectionStorage(actorSystem, storage, parallelism, batchingStrategy);
    }
}