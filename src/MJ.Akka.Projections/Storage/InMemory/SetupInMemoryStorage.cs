using System.Collections.Concurrent;

namespace MJ.Akka.Projections.Storage.InMemory;

public class SetupInMemoryStorage : IStorageSetup
{
    private readonly ConcurrentDictionary<ProjectionContextId, ReadOnlyMemory<byte>> _documents = new();
    
    public IProjectionStorage CreateProjectionStorage()
    {
        return new InMemoryProjectionStorage(_documents);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return new InMemoryPositionStorage();
    }

    internal ReadOnlyMemory<byte>? LoadDocument(ProjectionContextId id)
    {
        if (!_documents.TryGetValue(id, out var value))
            return null;

        return value;
    }

    internal void Clear()
    {
        _documents.Clear();
    }
}