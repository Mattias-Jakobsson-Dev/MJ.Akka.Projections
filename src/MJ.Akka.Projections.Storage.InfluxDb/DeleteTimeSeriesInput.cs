namespace MJ.Akka.Projections.Storage.InfluxDb;

public record DeleteTimeSeriesInput(DateTime Start, DateTime Stop, string Predicate);