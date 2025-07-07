using InfluxDB.Client.Writes;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public record InfluxDbWritePoint(InfluxDbTimeSeriesId Id, PointData Data) : IProjectionResult;