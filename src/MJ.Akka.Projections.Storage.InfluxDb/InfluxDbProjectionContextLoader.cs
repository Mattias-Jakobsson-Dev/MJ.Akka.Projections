namespace MJ.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionContextLoader : ILoadProjectionContext<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext>
{
    public Task<InfluxDbTimeSeriesContext> Load(InfluxDbTimeSeriesId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InfluxDbTimeSeriesContext(id));
    }
}