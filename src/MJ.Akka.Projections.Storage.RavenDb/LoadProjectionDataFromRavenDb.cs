using System.Collections.Immutable;
using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class LoadProjectionDataFromRavenDb<TDocument>(
    IDocumentStore documentStore, 
    Func<string, TDocument?> getDefaultDocument)
    : ILoadProjectionContext<string, RavenDbProjectionContext<TDocument>>
    where TDocument : class
{
    public async Task<RavenDbProjectionContext<TDocument>> Load(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var document = await session.LoadAsync<TDocument>(id, cancellationToken) ?? getDefaultDocument(id);
        
        var metadata = document != null 
            ? session.Advanced.GetMetadataFor(document).ToImmutableDictionary() 
            : ImmutableDictionary<string, object>.Empty;

        return new RavenDbProjectionContext<TDocument>(id, document, metadata);
    }
}