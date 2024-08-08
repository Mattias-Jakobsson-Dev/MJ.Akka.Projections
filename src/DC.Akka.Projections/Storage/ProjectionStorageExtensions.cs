using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public static class ProjectionStorageExtensions
{
    public static IProjectionStorage Batched(
        this IProjectionStorage storage,
        ActorSystem actorSystem,
        int batchSize = 100,
        int parallelism = 5)
    {
        return storage is BatchedProjectionStorage 
            ? storage 
            : new BatchedProjectionStorage(actorSystem, storage, batchSize, parallelism);
    }
}