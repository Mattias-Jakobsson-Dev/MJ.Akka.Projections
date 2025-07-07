using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxDbTimeSeriesContext(InfluxDbTimeSeriesId Id) : IProjectionContext
{
    public bool Exists()
    {
        return true;
    }
}