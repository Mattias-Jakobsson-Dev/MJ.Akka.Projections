namespace MJ.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionContextLoader : ILoadProjectionContext<InfluxDbTimeSeriesId, InfluxDbTimeSeries>
{
    public Task<InfluxDbTimeSeries> Load(InfluxDbTimeSeriesId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InfluxDbTimeSeries(id));
    }
}