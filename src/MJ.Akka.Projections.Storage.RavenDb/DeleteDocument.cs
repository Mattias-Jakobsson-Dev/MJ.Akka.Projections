namespace MJ.Akka.Projections.Storage.RavenDb;

public record DeleteDocument(string Id) : ICanBePersistedInRavenDb;