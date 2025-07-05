using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionContext<TId, TDocument>(TId id, TDocument? document) 
    : ProjectedDocumentContext<TId, TDocument>(id, document)
    where TId : notnull where TDocument : class
{
    private readonly bool _existed = document != null;
    
    public override PrepareForStorageResponse PrepareForStorage()
    {
        var results = ImmutableList<ICanBePersisted>.Empty;

        if (Exists())
            results = results.Add(new StoreDocumentInMemory(Id, Document));
        else if (_existed && !Exists())
            results = results.Add(new DeleteDocumentInMemory(Id));

        return new PrepareForStorageResponse(
            Id,
            results,
            new InMemoryProjectionContext<TId, TDocument>(Id, Document));
    }
}