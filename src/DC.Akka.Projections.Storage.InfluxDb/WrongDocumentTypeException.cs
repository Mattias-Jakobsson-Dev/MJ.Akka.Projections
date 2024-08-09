using System.Collections.Immutable;

namespace DC.Akka.Projections.Storage.InfluxDb;

public class WrongDocumentTypeException(IImmutableList<Type> documentTypes)
    : Exception(
        $"Can't store document of type {string.Join(", ", documentTypes.Select(x => x.Name))} using InfluxDb. Need type {typeof(InfluxTimeSeries)}");