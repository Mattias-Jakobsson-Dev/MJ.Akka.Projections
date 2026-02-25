using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public abstract class RavenDbProjection<TDocument> : RavenDbProjection<TDocument, SimpleIdContext<string>>
    where TDocument : class
{
    protected virtual TDocument? GetDefaultDocument(string id) => base.GetDefaultDocument(id);
}

[PublicAPI]
public abstract class RavenDbProjection<TDocument, TIdContext> 
    : BaseProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, SetupRavenDbStorage>
    where TDocument : class where TIdContext : IProjectionIdContext
{
    public override ILoadProjectionContext<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>> GetLoadProjectionContext(
        SetupRavenDbStorage storageSetup)
    {
        return new LoadProjectionDataFromRavenDb<TDocument, TIdContext>(storageSetup.GetDocumentStore());
    }
    
    protected virtual TDocument? GetDefaultDocument(TIdContext id) => null;

    public override RavenDbProjectionContext<TDocument, TIdContext> GetDefaultContext(TIdContext id)
    {
        return new RavenDbProjectionContext<TDocument, TIdContext>(
            id,
            GetDefaultDocument(id),
            ImmutableDictionary<string, object>.Empty);
    }
}