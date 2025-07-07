using System.Collections.Immutable;
using InfluxDB.Client.Writes;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public record InfluxDbWritePoint(InfluxDbTimeSeriesId Id, IImmutableList<PointData> Data) : IProjectionResult;