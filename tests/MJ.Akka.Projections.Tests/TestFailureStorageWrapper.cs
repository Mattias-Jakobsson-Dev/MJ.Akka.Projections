using System.Collections.Immutable;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests;

public class TestFailureStorageWrapper(IStorageSetup innerSetup, IImmutableList<StorageFailures> failures) 
    : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new FailStorage(innerSetup.CreateProjectionStorage(), failures);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return innerSetup.CreatePositionStorage();
    }
    
    private class FailStorage(
        IProjectionStorage innerStorage,
        IImmutableList<StorageFailures> failures) : IProjectionStorage
    {
        public Task Store(
            IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
            CancellationToken cancellationToken = default)
        {
            foreach (var failure in failures)
                failure.MaybeFail(contexts.Values.ToImmutableList());
            
            return innerStorage.Store(contexts, cancellationToken);
        }
    }
    
    public class Modifier(IImmutableList<StorageFailures> failures) : IModifyStorage
    {
        public IStorageSetup Modify(IStorageSetup source)
        {
            return new TestFailureStorageWrapper(source, failures);
        }
    }
}