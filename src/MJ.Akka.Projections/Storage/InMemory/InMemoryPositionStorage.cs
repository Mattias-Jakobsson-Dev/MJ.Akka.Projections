using System.Collections.Concurrent;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryPositionStorage : IProjectionPositionStorage
{
    private readonly ConcurrentDictionary<string, long> _positions = new();
    
    public virtual Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return _positions.TryGetValue(projectionName, out var position)
            ? Task.FromResult<long?>(position)
            : Task.FromResult<long?>(null);
    }

    public virtual Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (position != null)
        {
            _positions.AddOrUpdate(projectionName, _ => position.Value,
                (_, current) => current < position.Value ? position.Value : current);
        }

        return LoadLatestPosition(projectionName, cancellationToken);
    }
}