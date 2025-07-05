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
        public Task<StoreProjectionResponse> Store(
            StoreProjectionRequest request,
            CancellationToken cancellationToken = default)
        {
            var (_, items) = request.Take<ICanBePersisted>();
            
            foreach (var failure in failures)
                failure.MaybeFail(items);
            
            return innerStorage.Store(request, cancellationToken);
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