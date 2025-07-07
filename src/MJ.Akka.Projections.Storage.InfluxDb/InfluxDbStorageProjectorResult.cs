using System.Collections.Immutable;
using InfluxDB.Client.Writes;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public record InfluxDbStorageProjectorResult(
    IImmutableDictionary<InfluxDbTimeSeriesId, IImmutableList<PointData>> PointsToWrite,
    IImmutableList<InfluxDbDeletePoint> PointsToDelete)
{
    public static InfluxDbStorageProjectorResult Empty =>
        new(ImmutableDictionary<InfluxDbTimeSeriesId, IImmutableList<PointData>>.Empty,
            ImmutableList<InfluxDbDeletePoint>.Empty);
}