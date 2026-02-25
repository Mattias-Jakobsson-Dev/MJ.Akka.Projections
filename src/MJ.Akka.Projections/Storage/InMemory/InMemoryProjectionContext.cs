using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionContext<TIdContext, TDocument>(TIdContext id, TDocument? document) 
    : ContextWithDocument<TIdContext, TDocument>(id, document), IInMemoryProjectionContext
    where TIdContext : IProjectionIdContext where TDocument : class
{
    object? IInMemoryProjectionContext.Document => Document;
    
    public override IProjectionContext Freeze()
    {
        return new InMemoryProjectionContext<TIdContext, TDocument>(Id, Document);
    }
}

internal interface IInMemoryProjectionContext
{
    object? Document { get; }
}