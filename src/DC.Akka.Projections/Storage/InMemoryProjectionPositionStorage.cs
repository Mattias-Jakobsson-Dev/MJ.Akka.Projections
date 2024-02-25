using System.Collections.Concurrent;

namespace DC.Akka.Projections.Storage;

public class InMemoryProjectionPositionStorage : IProjectionPositionStorage
{
    protected readonly ConcurrentDictionary<string, long> Positions = new();
    
    public Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
    {
        return Positions.TryGetValue(projectionName, out var position)
            ? Task.FromResult<long?>(position)
            : Task.FromResult<long?>(default);
    }

    public Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        if (position != null)
            Positions.AddOrUpdate(projectionName, _ => position.Value, (_, _) => position.Value);

        return LoadLatestPosition(projectionName, cancellationToken);
    }
}