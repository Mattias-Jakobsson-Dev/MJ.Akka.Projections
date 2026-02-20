using System.Collections.Immutable;
using InfluxDB.Client;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionStorage(IInfluxDBClient client) : IProjectionStorage
{
    private readonly InProcessProjector<InfluxDbStorageProjectorResult> _storageProjector = InfluxDbStorageProjector
        .Setup();
    
    public async Task Store(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var invalidContexts = contexts
            .Where(x => x.Value is not InfluxDbTimeSeriesContext)
            .Select(x => x.Key)
            .ToImmutableList();

        if (!invalidContexts.IsEmpty)
            throw new InvalidContextStorageException(invalidContexts);

        var validContexts = contexts
            .Values
            .Select(x => x as InfluxDbTimeSeriesContext)
            .Where(x => x != null)
            .Select(x => x!)
            .ToImmutableList();
        
        var writeApi = client.GetWriteApiAsync();
        var deleteApi = client.GetDeleteApi();
        
        var (_, results) = _storageProjector
            .RunFor(
                validContexts
                    .SelectMany(x => x.Operations)
                    .OfType<object>()
                    .ToImmutableList(),
                InfluxDbStorageProjectorResult.Empty);

        foreach (var item in results.PointsToWrite)
        {
            if (item.Value.Any())
            {
                await writeApi
                    .WritePointsAsync(
                        item.Value.ToList(),
                        item.Key.Bucket,
                        item.Key.Organization,
                        cancellationToken);
            }
        }

        foreach (var deletePoint in results.PointsToDelete)
        {
            await deleteApi.Delete(
                deletePoint.Start,
                deletePoint.Stop,
                deletePoint.Predicate,
                deletePoint.Id.Bucket,
                deletePoint.Id.Organization,
                cancellationToken);
        }
    }
}