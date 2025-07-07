using System.Collections.Immutable;
using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.InMemory;

public record InMemoryStorageProjectorResult(
    IImmutableDictionary<object, object> DocumentsToUpsert,
    IImmutableList<object> DocumentsToDelete) : DocumentsStorageProjectorResult(DocumentsToUpsert, DocumentsToDelete)
{
    public static InMemoryStorageProjectorResult Empty
        => new(ImmutableDictionary<object, object>.Empty, ImmutableList<object>.Empty);
}