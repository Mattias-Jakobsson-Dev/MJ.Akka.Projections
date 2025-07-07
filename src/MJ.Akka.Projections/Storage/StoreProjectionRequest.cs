using System.Collections.Immutable;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage;

public record StoreProjectionRequest(IImmutableList<IProjectionResult> Results)
{
    public static StoreProjectionRequest Empty { get; } = new(ImmutableList<IProjectionResult>.Empty);
    
    public bool IsEmpty => Results.Count == 0;

    public StoreProjectionRequest MergeWith(StoreProjectionRequest other)
    {
        return new StoreProjectionRequest(Results.AddRange(other.Results).Distinct().ToImmutableList());
    }
}