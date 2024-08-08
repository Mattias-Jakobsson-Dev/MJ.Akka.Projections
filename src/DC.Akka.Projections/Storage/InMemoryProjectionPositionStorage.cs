using System.Collections.Concurrent;

namespace DC.Akka.Projections.Storage;

public class InMemoryProjectionPositionStorage : IProjectionPositionStorage
{
    private readonly ConcurrentDictionary<string, long> _positions = new();
    
    public Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
    {
        return _positions.TryGetValue(projectionName, out var position)
            ? Task.FromResult<long?>(position)
            : Task.FromResult<long?>(default);
    }

    public Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        if (position != null)
        {
            _positions.AddOrUpdate(projectionName, _ => position.Value,
                (_, current) => current < position.Value ? position.Value : current);
        }

        return LoadLatestPosition(projectionName, cancellationToken);
    }
}