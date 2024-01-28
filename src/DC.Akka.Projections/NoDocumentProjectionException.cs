namespace DC.Akka.Projections;

public class NoDocumentProjectionException<TDocument>(object id)
    : Exception($"Didn't find any document projection for type {typeof(TDocument)} with id {id}");