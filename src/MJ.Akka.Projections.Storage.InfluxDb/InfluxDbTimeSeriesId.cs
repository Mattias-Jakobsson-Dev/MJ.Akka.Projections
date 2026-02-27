using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxDbTimeSeriesId(string Bucket, string Organization, string Id) : IProjectionIdContext
{
    public virtual bool Equals(IProjectionIdContext? other)
    {
        return other is InfluxDbTimeSeriesId context &&
               Bucket == context.Bucket &&
               Organization == context.Organization &&
               Id == context.Id;
    }

    public string GetStringRepresentation()
    {
        return ToString();
    }

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