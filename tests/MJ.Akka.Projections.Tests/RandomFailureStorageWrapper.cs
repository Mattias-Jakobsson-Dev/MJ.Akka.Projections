using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests;

public class RandomFailureStorageWrapper(int failurePercentage, IStorageSetup innerStorage) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new Storage(failurePercentage, innerStorage.CreateProjectionStorage());
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return innerStorage.CreatePositionStorage();
    }
    
    private class Storage(int failurePercentage, IProjectionStorage innerStorage) : IProjectionStorage
    {
        private readonly Random _random = new();
    
        public Task<StoreProjectionResponse> Store(
            StoreProjectionRequest request, 
            CancellationToken cancellationToken = default)
        {
            if (_random.Next(100) <= failurePercentage)
                throw new Exception("Random failure");

            return innerStorage.Store(request, cancellationToken);
        }
    }
    
    public class Modifier(int failurePercentage) : IModifyStorage
    {
        public IStorageSetup Modify(IStorageSetup source)
        {
            return new RandomFailureStorageWrapper(failurePercentage, source);
        }
    }
}