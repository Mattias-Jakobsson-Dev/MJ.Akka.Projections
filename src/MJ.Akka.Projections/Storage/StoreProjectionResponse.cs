using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage;

public class StoreProjectionResponse
{
    private readonly IImmutableList<ICanBePersisted> _items;
    private readonly IImmutableList<Type> _taken;

    internal StoreProjectionResponse(
        IImmutableList<ICanBePersisted> items,
        IImmutableList<Type> taken)
    {
        _items = items;
        _taken = taken;
        
        Completed = items
            .All(x => taken.Any(y => y.IsInstanceOfType(x)));
    }
    
    internal static StoreProjectionResponse Empty { get; } 
        = new(ImmutableList<ICanBePersisted>.Empty, ImmutableList<Type>.Empty);

    public StoreProjectionResponse Merge(StoreProjectionResponse other)
    {
        return new StoreProjectionResponse(
            _items.AddRange(other._items).Distinct().ToImmutableList(),
            _taken.AddRange(other._taken).Distinct().ToImmutableList());
    }
    
    public bool Completed { get; }
}