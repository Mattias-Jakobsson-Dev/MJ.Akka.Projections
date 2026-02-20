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
        public Task Store(
            IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
            CancellationToken cancellationToken = default)
        {
            var events = contexts
                .Values
                .SelectMany(x => x switch
                {
                    ContextWithDocument<string, TestDocument<string>> { Document: { } testDoc } => testDoc.HandledEvents,
                    _ => ImmutableList<string>.Empty
                })
                .ToImmutableList();

            foreach (var evnt in events)
            {
                storedEvents.Add(evnt);
            }

            return innerStorage.Store(contexts, cancellationToken);
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