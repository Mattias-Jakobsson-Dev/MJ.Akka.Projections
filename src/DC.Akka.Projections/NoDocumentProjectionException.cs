namespace DC.Akka.Projections;

public class NoDocumentProjectionException<TDocument>(string projectionName)
    : Exception($"Didn't find any document projection for type {typeof(TDocument)} with name {projectionName}");

public class NoDocumentProjectionException(string projectionName)
    : Exception($"Didn't find any document projection with name {projectionName}");