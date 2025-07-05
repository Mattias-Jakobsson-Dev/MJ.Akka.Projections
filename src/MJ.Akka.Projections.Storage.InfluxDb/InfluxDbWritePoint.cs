using InfluxDB.Client.Writes;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public record InfluxDbWritePoint(InfluxDbTimeSeriesId Id, PointData Data) : ICanBePersisted;