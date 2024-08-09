using System.Collections.Immutable;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace DC.Akka.Projections.Storage.RavenDb;

public class RavenDbProjectionStorage(IDocumentStore documentStore) : IProjectionStorage
{
    public async Task<(TDocument? document, bool requireReload)> LoadDocument<TDocument>(
        object id,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        return (await session.LoadAsync<TDocument>(id.ToString(), cancellationToken), true);
    }

    public async Task Store(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        CancellationToken cancellationToken = default)
    {
        if (toUpsert.Any())
        {
            var bulkInsert = documentStore.BulkInsert(new BulkInsertOptions
            {
                SkipOverwriteIfUnchanged = true
            }, cancellationToken);

            foreach (var item in toUpsert)
                await bulkInsert.StoreAsync(item.Document, item.Id.ToString());

            await bulkInsert.DisposeAsync().ConfigureAwait(false);
        }

        if (toDelete.Any())
        {
            using var session = documentStore.OpenAsyncSession();

            foreach (var item in toDelete)
                session.Delete(item.Id.ToString());

            await session.SaveChangesAsync(cancellationToken);
        }
    }
}