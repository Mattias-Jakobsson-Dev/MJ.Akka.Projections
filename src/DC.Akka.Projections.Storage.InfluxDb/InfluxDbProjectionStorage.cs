using System.Collections.Immutable;
using Akka.Actor;
using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace DC.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionStorage(IInfluxDBClient client)
    : IProjectionStorage<InfluxDbTimeSeriesId, InfluxTimeSeries>
{
    public Task<(InfluxTimeSeries? document, bool requireReload)> LoadDocument(
        InfluxDbTimeSeriesId id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<(InfluxTimeSeries?, bool)>((new InfluxTimeSeries(
            ImmutableList<PointData>.Empty,
            ImmutableList<InfluxTimeSeries.DeletePoint>.Empty), true));
    }

    public Task<IStorageTransaction> StartTransaction(
        IImmutableList<(InfluxDbTimeSeriesId Id, InfluxTimeSeries Document, IActorRef ackTo)> toUpsert,
        IImmutableList<(InfluxDbTimeSeriesId id, IActorRef ackTo)> toDelete,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IStorageTransaction>(new InfluxDbStorageTransaction(toUpsert, client));
    }
}