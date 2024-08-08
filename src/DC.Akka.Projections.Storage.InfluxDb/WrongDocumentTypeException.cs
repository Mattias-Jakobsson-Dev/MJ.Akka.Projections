namespace DC.Akka.Projections.Storage.InfluxDb;

public class WrongDocumentTypeException(Type documentType) 
    : Exception($"Can't store document of type {documentType} using InfluxDb. Need type {typeof(InfluxTimeSeries)}");