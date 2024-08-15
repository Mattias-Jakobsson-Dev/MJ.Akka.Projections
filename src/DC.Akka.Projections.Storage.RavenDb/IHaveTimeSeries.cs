using System.Collections.Immutable;

namespace DC.Akka.Projections.Storage.RavenDb;

internal interface IHaveTimeSeries
{
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> TimeSeries { get; }
    object GetDocument();
}