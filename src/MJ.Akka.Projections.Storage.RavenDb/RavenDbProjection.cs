using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public abstract class RavenDbProjection<TDocument> 
    : BaseProjection<string, RavenDbProjectionContext<TDocument>, SetupRavenDbStorage>
    where TDocument : class
{
    public override ILoadProjectionContext<string, RavenDbProjectionContext<TDocument>> GetLoadProjectionContext(
        SetupRavenDbStorage storageSetup)
    {
        return new LoadProjectionDataFromRavenDb<TDocument>(storageSetup.GetDocumentStore());
    }
    
    protected virtual TDocument? GetDefaultDocument(string id) => null;

    public override RavenDbProjectionContext<TDocument> GetDefaultContext(string id)
    {
        return new RavenDbProjectionContext<TDocument>(
            id,
            GetDefaultDocument(id),
            ImmutableDictionary<string, object>.Empty);
    }
}