using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxDbTimeSeriesId(string Bucket, string Organization)
{
    public override string ToString()
    {
        return $"{Bucket}|{Organization}";
    }

    public static InfluxDbTimeSeriesId FromString(string id)
    {
        var parts = id.Split('|', 2);

        return new InfluxDbTimeSeriesId(parts[0], parts[1]);
    }
}