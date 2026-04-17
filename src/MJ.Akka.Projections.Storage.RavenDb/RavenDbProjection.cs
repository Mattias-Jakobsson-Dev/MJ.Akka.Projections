using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public abstract class RavenDbProjection<TDocument>
    : BaseProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, SetupRavenDbStorage>
    where TDocument : class
{
    public override ILoadProjectionContext<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>> GetLoadProjectionContext(
        SetupRavenDbStorage storageSetup)
    {
        return new LoadProjectionDataFromRavenDb<TDocument>(storageSetup.GetDocumentStore());
    }

    public override RavenDbProjectionContext<TDocument> GetDefaultContext(SimpleIdContext<string> id)
    {
        return new RavenDbProjectionContext<TDocument>(
             id,
             GetDefaultDocument(id),
             ImmutableDictionary<string, object>.Empty);
    }

    protected virtual TDocument? GetDefaultDocument(string id) => null;
}
