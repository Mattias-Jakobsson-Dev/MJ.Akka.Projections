using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class LoadProjectionDataFromRavenDb<TDocument>(IDocumentStore documentStore)
    : ILoadProjectionContext<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>>
    where TDocument : class
{
    public async Task<RavenDbProjectionContext<TDocument>> Load(
        SimpleIdContext<string> id,
        Func<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>> getDefaultContext,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var document = await session.LoadAsync<TDocument>(id.GetStringRepresentation(), cancellationToken);

        if (document == null)
            return getDefaultContext(id);
        
        var metadata = session.Advanced.GetMetadataFor(document).ToImmutableDictionary();

        return new RavenDbProjectionContext<TDocument>(id, document, metadata);
    }
}