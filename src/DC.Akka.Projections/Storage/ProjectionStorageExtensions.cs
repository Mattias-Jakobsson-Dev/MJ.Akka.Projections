using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public static class ProjectionStorageExtensions
{
    public static IProjectionStorage Batched(
        this IProjectionStorage storage,
        ActorSystem actorSystem,
        int batchSize,
        int parallelism)
    {
        return storage is BatchedProjectionStorage 
            ? storage 
            : new BatchedProjectionStorage(actorSystem, storage, batchSize, parallelism);
    }
}