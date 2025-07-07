using System.Collections.Immutable;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record StoreTimeSeries(string DocumentId, string Name, IImmutableList<TimeSeriesRecord> Records) 
    : IProjectionResult;