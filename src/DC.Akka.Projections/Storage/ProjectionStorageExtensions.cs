using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public static class ProjectionStorageExtensions
{
    public static BatchedProjectionStorage Batched(
        this IProjectionStorage storage,
        ActorSystem actorSystem,
        int batchSize,
        int parallelism)
    {
        return storage as BatchedProjectionStorage 
               ?? new BatchedProjectionStorage(actorSystem, storage, batchSize, parallelism);
    }
}