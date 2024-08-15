using System.Collections.Immutable;

namespace DC.Akka.Projections.Storage.RavenDb;

public record TimeSeriesRecord(DateTime TimeStamp, IImmutableList<double> Values, string? Tag);