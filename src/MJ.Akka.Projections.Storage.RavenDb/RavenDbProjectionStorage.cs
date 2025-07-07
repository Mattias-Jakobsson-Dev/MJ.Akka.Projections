using System.Collections.Immutable;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Json;

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
                var metadata = toStore
                    .MetadataToUpsert
                    .TryGetValue((string)item.Key, out var metadataValue)
                    ? metadataValue
                    : ImmutableDictionary<string, object>.Empty;

                await bulkInsert.StoreAsync(item.Value, (string)item.Key,
                    new MetadataAsDictionary(metadata.ToDictionary()));
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

        var metadataToUpsert = toStore.MetadataToUpsert
            .Where(kvp => !toStore.DocumentsToUpsert.ContainsKey(kvp.Key))
            .ToImmutableDictionary();

        if (!toStore.DocumentsToDelete.Any() && metadataToUpsert.IsEmpty)
            return StoreProjectionResponse.From(unhandledEvents);

        using var session = documentStore.OpenAsyncSession();

        foreach (var item in toStore.DocumentsToDelete)
            session.Delete((string)item);

        foreach (var metadataItem in metadataToUpsert)
        {
            session.Advanced.Defer(new PatchCommandData(
                metadataItem.Key,
                null,
                new PatchRequest
                {
                    Script = """
                             const metadata = getMetadata(this);

                             for (const prop in args.NewMetadata) {
                                metadata[prop] = args.NewMetadata[prop];
                             }
                             """,
                    Values = new Dictionary<string, object?>
                    {
                        ["NewMetadata"] = metadataItem.Value.ToDictionary()
                    }
                }));
        }

        await session.SaveChangesAsync(cancellationToken);

        return StoreProjectionResponse.From(unhandledEvents);
    }
}