using System.Collections.Immutable;
using InfluxDB.Client;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionStorage(IInfluxDBClient client) : IProjectionStorage
{
    private readonly InProcessProjector<InfluxDbStorageProjectorResult> _storageProjector = InfluxDbStorageProjector
        .Setup();
    
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var writeApi = client.GetWriteApiAsync();
        var deleteApi = client.GetDeleteApi();
        
        var (unhandledEvents, results) = _storageProjector
            .RunFor(
                request.Results.OfType<object>().ToImmutableList(),
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

        return StoreProjectionResponse.From(unhandledEvents);
    }
}