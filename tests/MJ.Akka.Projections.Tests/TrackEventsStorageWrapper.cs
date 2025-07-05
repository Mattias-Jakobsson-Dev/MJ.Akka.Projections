using System.Collections.Concurrent;
using System.Collections.Immutable;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;

namespace MJ.Akka.Projections.Tests;

public class TrackEventsStorageWrapper(ConcurrentBag<string> storedEvents, IStorageSetup innerSetup) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new Storage(storedEvents, innerSetup.CreateProjectionStorage());
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return innerSetup.CreatePositionStorage();
    }
    
    private class Storage(ConcurrentBag<string> storedEvents, IProjectionStorage innerStorage) : IProjectionStorage
    {
        public Task<StoreProjectionResponse> Store(
            StoreProjectionRequest request,
            CancellationToken cancellationToken = default)
        {
            var (_, items) = request.Take<StoreDocumentInMemory>();
            
            var events = items
                .Select(x => x.Document as TestDocument<string>)
                .Where(x => x != null)
                .SelectMany(x => x!.HandledEvents)
                .ToImmutableList();

            foreach (var evnt in events)
            {
                storedEvents.Add(evnt);
            }

            return innerStorage.Store(request, cancellationToken);
        }
    }
    
    public class Modifier : IModifyStorage
    {
        public ConcurrentBag<string> StoredEvents { get; } = new();
        
        public IStorageSetup Modify(IStorageSetup source)
        {
            return new TrackEventsStorageWrapper(StoredEvents, source);
        }
    }
}