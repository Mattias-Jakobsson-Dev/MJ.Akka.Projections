using System.Collections.Immutable;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxTimeSeries(
    IImmutableList<PointData> Points,
    IImmutableList<InfluxTimeSeries.DeletePoint> ToDelete) : IResetDocument<InfluxTimeSeries>
{
    public InfluxTimeSeries AddPoint(PointData point)
    {
        return this with { Points = Points.Add(point) };
    }

    public InfluxTimeSeries Delete(DateTime start, DateTime stop, string predicate)
    {
        return this with { ToDelete = ToDelete.Add(new DeletePoint(start, stop, predicate)) };
    }
    
    public record DeletePoint(DateTime Start, DateTime Stop, string Predicate);

    public InfluxTimeSeries Reset()
    {
        return new InfluxTimeSeries(
            ImmutableList<PointData>.Empty,
            ImmutableList<DeletePoint>.Empty);
    }
}