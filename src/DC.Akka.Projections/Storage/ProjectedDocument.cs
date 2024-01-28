namespace DC.Akka.Projections.Storage;

public record ProjectedDocument(object Id, object? Document, long Position);