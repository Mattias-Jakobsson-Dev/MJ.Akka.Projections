using System.Collections.Concurrent;
using System.Collections.Immutable;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Storage;
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
            var events = request
                .Results
                .SelectMany(x => x switch
                {
                    DocumentResults.DocumentModified { Document: TestDocument<string> testDoc } => testDoc.HandledEvents,
                    DocumentResults.DocumentCreated { Document: TestDocument<string> testDoc } => testDoc.HandledEvents,
                    _ => ImmutableList<string>.Empty
                })
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