using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InMemory;

public abstract class InMemoryProjection<TIdContext, TDocument> 
    : BaseProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, SetupInMemoryStorage>
    where TIdContext : IProjectionIdContext where TDocument : class
{
    public override ILoadProjectionContext<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>> GetLoadProjectionContext(
        SetupInMemoryStorage storageSetup)
    {
        return new InMemoryProjectionLoader<TIdContext, TDocument>(
            id => storageSetup.LoadDocument(new ProjectionContextId(Name, id)));
    }
    
    [PublicAPI]
    protected virtual TDocument? GetDefaultDocument(TIdContext id) => null;

    public override InMemoryProjectionContext<TIdContext, TDocument> GetDefaultContext(TIdContext id)
    {
        return new InMemoryProjectionContext<TIdContext, TDocument>(
            id,
            GetDefaultDocument(id));
    }
}