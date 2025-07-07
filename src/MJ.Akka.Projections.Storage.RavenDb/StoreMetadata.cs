using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record StoreMetadata(string DocumentId, string Key, object Value) : IProjectionResult;