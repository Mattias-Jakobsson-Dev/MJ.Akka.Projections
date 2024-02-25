using System.Collections.Immutable;
using Akka.Actor;
using InfluxDB.Client;

namespace DC.Akka.Projections.Storage.InfluxDb;

public record InfluxDbStorageTransaction(
    IImmutableList<(InfluxDbTimeSeriesId id, InfluxTimeSeries document, IActorRef ackTo)> Items,
    IInfluxDBClient Client)
    : IStorageTransaction
{
    public IStorageTransaction MergeWith(
        IStorageTransaction transaction,
        Func<IStorageTransaction, IStorageTransaction, IStorageTransaction> defaultMerge)
    {
        if (transaction is InfluxDbStorageTransaction otherTransaction)
        {
            return this with
            {
                Items = Items.AddRange(otherTransaction.Items)
            };
        }

        return defaultMerge(this, transaction);
    }

    public async Task Commit(CancellationToken cancellationToken = default)
    {
        var destinations = Items
            .GroupBy(x => x.id);

        var writeApi = Client.GetWriteApiAsync();
        var deleteApi = Client.GetDeleteApi();

        foreach (var destination in destinations)
        {
            var pointsToAdd = destination
                .SelectMany(x => x.document.Points)
                .ToImmutableList();

            var pointsToDelete = destination
                .SelectMany(x => x.document.ToDelete)
                .ToImmutableList();
            
            try
            {
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

                foreach (var item in destination)
                    item.ackTo.Tell(new Messages.Acknowledge());
            }
            catch (Exception e)
            {
                foreach (var item in destination)
                    item.ackTo.Tell(new Messages.Reject(e));
            }
        }
    }
}