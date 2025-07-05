using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public abstract class InfluxDbProjection : BaseProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeries, SetupInfluxDbStorage>
{
    public override InfluxDbTimeSeriesId IdFromString(string id)
    {
        return id;
    }

    public override string IdToString(InfluxDbTimeSeriesId id)
    {
        return id;
    }
    
    public override ILoadProjectionContext<InfluxDbTimeSeriesId, InfluxDbTimeSeries> GetLoadProjectionContext(
        SetupInfluxDbStorage storageSetup)
    {
        return new InfluxDbProjectionContextLoader();
    }
}