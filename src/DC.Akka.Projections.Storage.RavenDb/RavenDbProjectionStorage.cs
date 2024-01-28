using System.Collections.Immutable;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace DC.Akka.Projections.Storage.RavenDb;

public class RavenDbProjectionStorage(IDocumentStore documentStore) : IProjectionStorage
{
    public async Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        return await session.LoadAsync<TDocument>(id.ToString(), cancellationToken);
    }

    public async Task StoreDocuments(
        IImmutableList<(ProjectedDocument Document, Action Ack, Action<Exception?> Nack)> documents,
        CancellationToken cancellationToken = default)
    {
        var documentsToInsert = documents.Where(x => x.Document.Document != null).ToImmutableList();
        
        try
        {
            var bulkInsert = documentStore.BulkInsert(new BulkInsertOptions
            {
                SkipOverwriteIfUnchanged = true
            }, cancellationToken);

            foreach (var item in documentsToInsert)
                await bulkInsert.StoreAsync(item.Document, item.Document.Id.ToString());
            
            await bulkInsert.DisposeAsync().ConfigureAwait(false);

            foreach (var item in documentsToInsert)
                item.Ack();
        }
        catch (Exception e)
        {
            foreach (var item in documentsToInsert)
                item.Nack(e);
        }

        var deletedDocuments = documents.Where(x => x.Document.Document == null).ToImmutableList();
        
        if (deletedDocuments.IsEmpty)
            return;
        
        try
        {
            using var session = documentStore.OpenAsyncSession();

            foreach (var item in deletedDocuments)
                session.Delete(item.Document.Id.ToString());

            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            foreach (var item in deletedDocuments)
                item.Nack(e);
        }
    }

    public async Task<long?> LoadLatestPosition(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var projectionPosition = await session.LoadAsync<ProjectionPosition>(
            ProjectionPosition.BuildId(projectionName),
            cancellationToken);

        return projectionPosition?.Position;
    }

    public async Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();
        
        var projectionPosition = await session.LoadAsync<ProjectionPosition>(
            ProjectionPosition.BuildId(projectionName), 
            cancellationToken);

        if (projectionPosition == null)
        {
            projectionPosition = new ProjectionPosition();

            await session.StoreAsync(projectionPosition, cancellationToken);
        }

        if (position != null && position > projectionPosition.Position)
            projectionPosition.Position = position.Value;

        await session.SaveChangesAsync(cancellationToken);

        return projectionPosition.Position;
    }
}