namespace DC.Akka.Projections.Storage.InfluxDb;

public class InvalidInfluxDocumentTypeException(Type requestedType) 
    : Exception($"The requested type {requestedType} is not supported by the InfluxDb storage");