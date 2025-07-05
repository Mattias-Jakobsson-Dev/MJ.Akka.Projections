using System.Collections.Immutable;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxDbTimeSeries(
    InfluxDbTimeSeriesId Id,
    IImmutableList<PointData> Points,
    IImmutableList<InfluxDbDeletePoint> ToDelete) 
    : IProjectionContext<InfluxDbTimeSeriesId>
{
    public InfluxDbTimeSeries(InfluxDbTimeSeriesId id) 
        : this(id, ImmutableList<PointData>.Empty, ImmutableList<InfluxDbDeletePoint>.Empty)
    {
        
    }
    
    public InfluxDbTimeSeries AddPoint(PointData point)
    {
        return this with { Points = Points.Add(point) };
    }

    public InfluxDbTimeSeries Delete(DateTime start, DateTime stop, string predicate)
    {
        return this with { ToDelete = ToDelete.Add(new InfluxDbDeletePoint(Id, start, stop, predicate)) };
    }
    
    public bool Exists()
    {
        return true;
    }

    public PrepareForStorageResponse PrepareForStorage()
    {
        return new PrepareForStorageResponse(
            Id,
            Points.Select(ICanBePersisted (x) => new InfluxDbWritePoint(Id, x)).ToImmutableList().AddRange(ToDelete),
            new InfluxDbTimeSeries(Id));
    }
}