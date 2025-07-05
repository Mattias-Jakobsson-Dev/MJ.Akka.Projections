using System.Collections.Immutable;
using InfluxDB.Client;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionStorage(IInfluxDBClient client) : IProjectionStorage
{
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var writeApi = client.GetWriteApiAsync();
        var deleteApi = client.GetDeleteApi();

        (request, var itemsToWrite) = request
            .Take<InfluxDbWritePoint>();
        
        var writeItems = itemsToWrite
            .GroupBy(x => x.Id)
            .ToImmutableDictionary(
                x => x.Key,
                x => x.ToImmutableList());

        foreach (var item in writeItems)
        {
            if (!item.Value.IsEmpty)
            {
                await writeApi
                    .WritePointsAsync(
                        item.Value.Select(x => x.Data).ToList(),
                        item.Key.Bucket,
                        item.Key.Organization,
                        cancellationToken);
            }
        }
        
        (request, var deleteItems) = request
            .Take<InfluxDbDeletePoint>();
        
        foreach (var deletePoint in deleteItems)
        {
            await deleteApi.Delete(
                deletePoint.Start,
                deletePoint.Stop,
                deletePoint.Predicate,
                deletePoint.Id.Bucket,
                deletePoint.Id.Organization,
                cancellationToken);
        }

        return request.ToResponse();
    }
}