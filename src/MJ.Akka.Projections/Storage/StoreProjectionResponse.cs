using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage;

[PublicAPI]
public record StoreProjectionResponse(IImmutableList<Type> UnhandledEventTypes)
{
    public static StoreProjectionResponse From(IImmutableList<object> unhandledEvents)
    {
        return new StoreProjectionResponse(unhandledEvents
            .Select(x => x.GetType())
            .Distinct()
            .ToImmutableList());
    }
    
    public bool Completed => !UnhandledEventTypes.Any();
    
    public StoreProjectionResponse MergeWith(StoreProjectionResponse other)
    {
        return new StoreProjectionResponse(UnhandledEventTypes.Intersect(other.UnhandledEventTypes).ToImmutableList());
    }
}
