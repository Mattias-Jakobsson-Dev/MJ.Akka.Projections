using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public record InfluxDbDeletePoint(InfluxDbTimeSeriesId Id, DateTime Start, DateTime Stop, string Predicate) 
    : IProjectionResult;