using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionContext<TId, TDocument>(TId id, TDocument? document) 
    : ContextWithDocument<TId, TDocument>(id, document), IInMemoryProjectionContext
    where TId : notnull where TDocument : class
{
    object? IInMemoryProjectionContext.Document => Document;
    
    public override IProjectionContext Freeze()
    {
        return new InMemoryProjectionContext<TId, TDocument>(Id, Document);
    }
}

internal interface IInMemoryProjectionContext
{
    object? Document { get; }
}