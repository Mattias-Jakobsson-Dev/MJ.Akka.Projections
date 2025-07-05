namespace MJ.Akka.Projections.Storage.RavenDb;

public record PersistDocument(string Id, object Document) : ICanBePersistedInRavenDb;