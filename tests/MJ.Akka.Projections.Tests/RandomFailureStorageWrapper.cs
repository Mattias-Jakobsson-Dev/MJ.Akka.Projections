using System.Collections.Immutable;
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
    
        public Task Store(
            IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts, 
            CancellationToken cancellationToken = default)
        {
            if (_random.Next(100) <= failurePercentage)
                throw new Exception("Random failure");

            return innerStorage.Store(contexts, cancellationToken);
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