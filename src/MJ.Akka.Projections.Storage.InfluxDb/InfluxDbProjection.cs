using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public abstract class InfluxDbProjection : BaseProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, SetupInfluxDbStorage>
{
    public override ILoadProjectionContext<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext> GetLoadProjectionContext(
        SetupInfluxDbStorage storageSetup)
    {
        return new InfluxDbProjectionContextLoader();
    }
}