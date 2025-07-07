namespace MJ.Akka.Projections.Storage.InMemory;

public abstract class InMemoryProjection<TId, TDocument> 
    : BaseProjection<TId, InMemoryProjectionContext<TId, TDocument>, SetupInMemoryStorage>
    where TId : notnull where TDocument : class
{
    public override ILoadProjectionContext<TId, InMemoryProjectionContext<TId, TDocument>> GetLoadProjectionContext(
        SetupInMemoryStorage storageSetup)
    {
        return new InMemoryProjectionLoader<TId, TDocument>(id => storageSetup.LoadDocument(id));
    }
}