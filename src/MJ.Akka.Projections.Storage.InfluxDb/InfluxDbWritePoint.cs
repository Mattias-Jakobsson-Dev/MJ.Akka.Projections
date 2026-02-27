using System.Collections.Immutable;
using InfluxDB.Client.Writes;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public record InfluxDbWritePoint(InfluxDbTimeSeriesId Id, IImmutableList<PointData> Data) : IInfluxDbOperation;