using Akka.Actor;

namespace MJ.Akka.Projections.Storage.Batched;

public class AddBatchedProjectionStorage(
    IStorageSetup innerSetup,
    ActorSystem actorSystem,
    int parallelism,
    IStorageBatchingStrategy batchingStrategy) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        if (innerSetup is AddBatchedProjectionStorage)
            return innerSetup.CreateProjectionStorage();

        return new BatchedProjectionStorage(
            actorSystem,
            innerSetup.CreateProjectionStorage(),
            parallelism,
            batchingStrategy);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return innerSetup.CreatePositionStorage();
    }
}