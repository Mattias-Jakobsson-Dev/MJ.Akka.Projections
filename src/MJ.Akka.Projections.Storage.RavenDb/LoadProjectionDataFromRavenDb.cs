using System.Collections.Immutable;
using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class LoadProjectionDataFromRavenDb<TDocument>(IDocumentStore documentStore)
    : ILoadProjectionContext<string, RavenDbProjectionContext<TDocument>>
    where TDocument : class
{
    public async Task<RavenDbProjectionContext<TDocument>> Load(
        string id,
        Func<string, RavenDbProjectionContext<TDocument>> getDefaultContext,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var document = await session.LoadAsync<TDocument>(id, cancellationToken);

        if (document == null)
            return getDefaultContext(id);
        
        var metadata = session.Advanced.GetMetadataFor(document).ToImmutableDictionary();

        return new RavenDbProjectionContext<TDocument>(id, document, metadata);
    }
}