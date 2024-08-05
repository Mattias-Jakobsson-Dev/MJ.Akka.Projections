using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public record InfluxDbTimeSeriesId(string Bucket, string Organization);