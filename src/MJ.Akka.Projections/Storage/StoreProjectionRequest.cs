using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage;

public record StoreProjectionRequest(
    IImmutableList<ICanBePersisted> ItemsToStore,
    IImmutableList<Type> Taken)
{
    public StoreProjectionRequest(IImmutableList<ICanBePersisted> itemsToStore) 
        : this(itemsToStore, ImmutableList<Type>.Empty)
    {
        
    }
    
    public static StoreProjectionRequest Empty { get; } = new(ImmutableList<ICanBePersisted>.Empty);
    
    public bool IsEmpty => ItemsToStore.Count == 0;

    public StoreProjectionRequest MergeWith(StoreProjectionRequest other)
    {
        return new StoreProjectionRequest(
            ItemsToStore.AddRange(other.ItemsToStore).Distinct().ToImmutableList(), 
            Taken.AddRange(other.Taken).Distinct().ToImmutableList());
    }
    
    public (StoreProjectionRequest request, IImmutableList<T> items) Take<T>() 
        where T : class, ICanBePersisted
    {
        return (this with
        {
            Taken = Taken.Add(typeof(T))
        }, ItemsToStore.OfType<T>().ToImmutableList());
    }
    
    public StoreProjectionResponse ToResponse()
    {
        return new StoreProjectionResponse(ItemsToStore, Taken);
    }
}