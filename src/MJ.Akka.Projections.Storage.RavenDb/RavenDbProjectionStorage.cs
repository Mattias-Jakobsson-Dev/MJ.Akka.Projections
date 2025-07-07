using System.Collections.Immutable;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class RavenDbDocumentProjectionStorage(IDocumentStore documentStore, BulkInsertOptions insertOptions)
    : IProjectionStorage
{
    private readonly InProcessProjector<RavenDbStorageProjectorResult> _storageProjector = RavenDbStorageProjector
        .Setup();
    
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var (unhandledEvents, toStore) = _storageProjector.RunFor(
            request.Results.OfType<object>().ToImmutableList(),
            RavenDbStorageProjectorResult.Empty);

        if (toStore.DocumentsToUpsert.Any() || toStore.TimeSeriesToAdd.Any())
        {
            await using var bulkInsert = documentStore.BulkInsert(insertOptions, cancellationToken);

            foreach (var item in toStore.DocumentsToUpsert)
            {
                await bulkInsert.StoreAsync(item.Value, (string)item.Key);
            }
            
            foreach (var timeSeriesDocument in toStore.TimeSeriesToAdd)
            {
                foreach (var timeSeries in timeSeriesDocument.Value)
                {
                    using var timeSeriesBatch = bulkInsert.TimeSeriesFor(
                        timeSeriesDocument.Key,
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

        if (!toStore.DocumentsToDelete.Any())
            return StoreProjectionResponse.From(unhandledEvents);

        using var session = documentStore.OpenAsyncSession();

        foreach (var item in toStore.DocumentsToDelete)
            session.Delete((string)item);

        await session.SaveChangesAsync(cancellationToken);

        return StoreProjectionResponse.From(unhandledEvents);
    }
}