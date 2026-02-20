namespace MJ.Akka.Projections.Storage.InfluxDb;

public class InfluxDbProjectionContextLoader : ILoadProjectionContext<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext>
{
    public Task<InfluxDbTimeSeriesContext> Load(
        InfluxDbTimeSeriesId id,
        Func<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext> getDefaultContext,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InfluxDbTimeSeriesContext(id));
    }
}