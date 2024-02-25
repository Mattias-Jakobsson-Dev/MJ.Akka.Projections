namespace DC.Akka.Projections;

public class NoDocumentProjectionException<TId, TDocument>(TId id)
    : Exception($"Didn't find any document projection for type {typeof(TDocument)} with id {id}");
    
public class NoDocumentProjectionException<TDocument>(string projectionName)
    : Exception($"Didn't find any document projection for type {typeof(TDocument)} with name {projectionName}");