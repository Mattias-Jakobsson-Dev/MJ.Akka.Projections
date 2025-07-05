using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record StoreTimeSeries(string DocumentId, string Name, IImmutableList<TimeSeriesRecord> Records) 
    : ICanBePersistedInRavenDb;