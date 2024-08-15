using System.Collections.Immutable;
using System.Reflection;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace DC.Akka.Projections.Storage.RavenDb;

public class RavenDbProjectionStorage(IDocumentStore documentStore, BulkInsertOptions insertOptions)
    : IProjectionStorage
{
    public async Task<TDocument?> LoadDocument<TDocument>(
        object id,
        CancellationToken cancellationToken = default)
    {
        if (typeof(TDocument).IsOfGenericType(typeof(DocumentWithTimeSeries<>)))
        {
            var documentType = typeof(TDocument).GenericTypeArguments[0];

            return await (Task<TDocument?>)typeof(RavenDbProjectionStorage)
                .GetMethod(nameof(LoadTimeSeries), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(documentType)
                .Invoke(this, [id, cancellationToken])!;
        }

        using var session = documentStore.OpenAsyncSession();

        return await session.LoadAsync<TDocument>(id.ToString(), cancellationToken);
    }

    public async Task Store(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        CancellationToken cancellationToken = default)
    {
        if (toUpsert.Any())
        {
            await using var bulkInsert = documentStore.BulkInsert(insertOptions, cancellationToken);

            foreach (var item in toUpsert)
            {
                if (item.Document is IHaveTimeSeries timeSeriesDocument)
                {
                    await bulkInsert.StoreAsync(timeSeriesDocument.GetDocument(), item.Id.ToString());

                    foreach (var timeSeries in timeSeriesDocument.TimeSeries)
                    {
                        if (!timeSeries.Value.Any())
                            continue;

                        using var timeSeriesBatch = bulkInsert.TimeSeriesFor(
                            item.Id.ToString(),
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
                else
                {
                    await bulkInsert.StoreAsync(item.Document, item.Id.ToString());
                }
            }
        }

        if (toDelete.Any())
        {
            using var session = documentStore.OpenAsyncSession();

            foreach (var item in toDelete)
                session.Delete(item.Id.ToString());

            await session.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<DocumentWithTimeSeries<TDocument>?> LoadTimeSeries<TDocument>(
        object id,
        CancellationToken cancellationToken)
        where TDocument : notnull
    {
        var document = await LoadDocument<TDocument>(id, cancellationToken);

        return document == null
            ? null
            : DocumentWithTimeSeries<TDocument>.FromDocument(document);
    }
}