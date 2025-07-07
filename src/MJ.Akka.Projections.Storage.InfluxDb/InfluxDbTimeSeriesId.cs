using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxDbTimeSeriesId(string Bucket, string Organization, string Id)
{
    public override string ToString()
    {
        return $"{Bucket}|{Organization}|{Id}";
    }

    private static InfluxDbTimeSeriesId FromString(string id)
    {
        var parts = id.Split('|', 3);

        return new InfluxDbTimeSeriesId(parts[0], parts[1], parts[2]);
    }

    public static implicit operator string(InfluxDbTimeSeriesId item) => item.ToString();

    public static implicit operator InfluxDbTimeSeriesId(string item) => FromString(item);
}