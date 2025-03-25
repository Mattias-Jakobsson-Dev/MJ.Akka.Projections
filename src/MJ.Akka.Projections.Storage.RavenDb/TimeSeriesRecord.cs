using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record TimeSeriesRecord(DateTime TimeStamp, IImmutableList<double> Values, string? Tag);