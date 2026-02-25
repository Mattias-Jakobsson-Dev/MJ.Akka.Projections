using System.Collections.Immutable;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Json;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class RavenDbDocumentProjectionStorage(IDocumentStore documentStore, BulkInsertOptions insertOptions)
    : IProjectionStorage
{
    public async Task Store(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var invalidContexts = contexts
            .Where(x => x.Value is not IRavenDbProjectionContext)
            .Select(x => x.Key)
            .ToImmutableList();

        if (!invalidContexts.IsEmpty)
            throw new InvalidContextStorageException(invalidContexts);
        
        var validContexts = contexts
            .Values
            .Select(x => x as IRavenDbProjectionContext)
            .Where(x => x != null)
            .Select(x => x!)
            .ToImmutableList();

        var documentsToUpsert = validContexts
            .Where(x => x.Document != null)
            .ToImmutableList();

        var timeSeriesToAdd = validContexts
            .Where(x => x.AddedTimeSeries.Any())
            .ToImmutableList();
        
        if (!documentsToUpsert.IsEmpty || !timeSeriesToAdd.IsEmpty)
        {
            await using var bulkInsert = documentStore.BulkInsert(insertOptions, cancellationToken);

            foreach (var item in documentsToUpsert)
            {
                await bulkInsert.StoreAsync(item.Document, item.GetDocumentId(),
                    new MetadataAsDictionary(item.Metadata.ToDictionary()));
            }

            foreach (var timeSeriesDocument in timeSeriesToAdd)
            {
                foreach (var timeSeries in timeSeriesDocument.AddedTimeSeries)
                {
                    using var timeSeriesBatch = bulkInsert.TimeSeriesFor(
                        timeSeriesDocument.GetDocumentId(),
                        timeSeries.Key);

                    foreach (var timeSeriesRecord in timeSeries.Value)
                    {
                        await timeSeriesBatch
                            .AppendAsync(
                                timeSeriesRecord.TimeStamp,
                                timeSeriesRecord.Values.ToList(),
                                timeSeriesRecord.Tag);
                    }
                }
            }
        }
        
        var documentsToDelete = validContexts
            .Where(x => x.Document == null)
            .Select(x => x.GetDocumentId())
            .ToImmutableList();

        if (documentsToDelete.IsEmpty)
            return;

        using var session = documentStore.OpenAsyncSession();

        foreach (var item in documentsToDelete)
            session.Delete(item);

        await session.SaveChangesAsync(cancellationToken);
    }
}