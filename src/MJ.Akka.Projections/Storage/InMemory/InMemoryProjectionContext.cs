using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionContext<TId, TDocument>(SimpleIdContext<TId> id, TDocument? document)
    : ContextWithDocument<SimpleIdContext<TId>, TDocument>(id, document), IInMemoryProjectionContext
    where TDocument : class
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