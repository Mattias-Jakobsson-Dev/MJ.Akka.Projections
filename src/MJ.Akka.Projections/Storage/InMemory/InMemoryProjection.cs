using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InMemory;

public abstract class InMemoryProjection<TId, TDocument>
    : BaseProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, SetupInMemoryStorage>
    where TDocument : class
{
    public override ILoadProjectionContext<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>> GetLoadProjectionContext(
        SetupInMemoryStorage storageSetup)
    {
        return new InMemoryProjectionLoader<TId, TDocument>(
            id => storageSetup.LoadDocument(new ProjectionContextId(Name, id)));
    }

    [PublicAPI]
    protected virtual TDocument? GetDefaultDocument(SimpleIdContext<TId> id) => null;

    public override InMemoryProjectionContext<TId, TDocument> GetDefaultContext(SimpleIdContext<TId> id)
    {
        return new InMemoryProjectionContext<TId, TDocument>(
            id,
            GetDefaultDocument(id));
    }
}