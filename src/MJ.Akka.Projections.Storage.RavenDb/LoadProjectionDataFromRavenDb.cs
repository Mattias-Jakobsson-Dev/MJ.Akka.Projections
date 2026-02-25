using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class LoadProjectionDataFromRavenDb<TDocument, TIdContext>(IDocumentStore documentStore)
    : ILoadProjectionContext<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>>
    where TDocument : class where TIdContext : IProjectionIdContext
{
    public async Task<RavenDbProjectionContext<TDocument, TIdContext>> Load(
        TIdContext id,
        Func<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>> getDefaultContext,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var document = await session.LoadAsync<TDocument>(id.GetStringRepresentation(), cancellationToken);

        if (document == null)
            return getDefaultContext(id);
        
        var metadata = session.Advanced.GetMetadataFor(document).ToImmutableDictionary();

        return new RavenDbProjectionContext<TDocument, TIdContext>(id, document, metadata);
    }
}