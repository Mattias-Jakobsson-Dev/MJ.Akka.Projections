using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class LoadProjectionDataFromRavenDb<TDocument>(IDocumentStore documentStore)
    : ILoadProjectionContext<string, RavenDbProjectionContext<TDocument>>
    where TDocument : class
{
    public async Task<RavenDbProjectionContext<TDocument>> Load(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var document = await session.LoadAsync<TDocument>(id, cancellationToken);

        return new RavenDbProjectionContext<TDocument>(id, document);
    }
}