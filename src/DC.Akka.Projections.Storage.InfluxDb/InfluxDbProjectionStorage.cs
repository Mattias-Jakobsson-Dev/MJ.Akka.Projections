using System.Collections.Immutable;
using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace DC.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionStorage(IInfluxDBClient client)
    : IProjectionStorage
{
    public Task<TDocument?> LoadDocument<TDocument>(
        object id,
        CancellationToken cancellationToken = default)
    {
        if (!typeof(InfluxTimeSeries).IsAssignableFrom(typeof(TDocument)))
            return Task.FromResult<TDocument?>(default);

        object emptyTimeSeries = new InfluxTimeSeries(
            ImmutableList<PointData>.Empty,
            ImmutableList<InfluxTimeSeries.DeletePoint>.Empty);

        return Task.FromResult<TDocument?>((TDocument)emptyTimeSeries);
    }

    public async Task Store(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        CancellationToken cancellationToken = default)
    {
        var items = toUpsert
            .Select(x => new
            {
                Id = x.Id as InfluxDbTimeSeriesId,
                TimeSeries = x.Document as InfluxTimeSeries,
                Source = x
            })
            .ToImmutableList();

        var wrongTypes = items
            .Where(x => x.Id == null || x.TimeSeries == null)
            .ToImmutableList();

        if (!wrongTypes.IsEmpty)
            throw new WrongDocumentTypeException(wrongTypes.Select(x => x.Source.Document.GetType()).ToImmutableList());
        
        var destinations = items
            .Where(x => x.Id != null && x.TimeSeries != null)
            .Select(x => new
            {
                Id = x.Id!,
                TimeSeries = x.TimeSeries!,
                x.Source
            })
            .GroupBy(x => x.Id);

        var writeApi = client.GetWriteApiAsync();
        var deleteApi = client.GetDeleteApi();

        foreach (var destination in destinations)
        {
            var pointsToAdd = destination
                .SelectMany(x => x.TimeSeries.Points)
                .ToImmutableList();

            var pointsToDelete = destination
                .SelectMany(x => x.TimeSeries.ToDelete)
                .ToImmutableList();

            if (!pointsToAdd.IsEmpty)
            {
                await writeApi
                    .WritePointsAsync(
                        pointsToAdd.ToList(),
                        destination.Key.Bucket,
                        destination.Key.Organization,
                        cancellationToken);
            }

            foreach (var deletePoint in pointsToDelete)
            {
                await deleteApi.Delete(
                    deletePoint.Start,
                    deletePoint.Stop,
                    deletePoint.Predicate,
                    destination.Key.Bucket,
                    destination.Key.Organization,
                    cancellationToken);
            }
        }
    }
}