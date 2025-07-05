using System.Collections.Immutable;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class RavenDbDocumentProjectionStorage(IDocumentStore documentStore, BulkInsertOptions insertOptions)
    : IProjectionStorage
{
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var documents =
            new Dictionary<string, (object doc, IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>
                timeSeries)>();

        var deletedDocs = new List<string>();

        (request, var items) = request
            .Take<ICanBePersistedInRavenDb>();

        foreach (var item in items)
        {
            switch (item)
            {
                case PersistDocument persist:
                    if (documents.TryGetValue(persist.Id, out var persisted))
                    {
                        documents[persist.Id] = (persist.Document, persisted.timeSeries);
                    }
                    else
                    {
                        documents[persist.Id] = (
                            persist.Document,
                            ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty);
                    }

                    deletedDocs.Remove(persist.Id);

                    break;
                case StoreTimeSeries timeSeries:
                    if (documents.TryGetValue(timeSeries.DocumentId, out var value))
                    {
                        var timeSeriesData = value.timeSeries.SetItem(
                            timeSeries.Name,
                            value.timeSeries.TryGetValue(
                                timeSeries.Name,
                                out var timeSeriesValue)
                                ? timeSeriesValue.AddRange(timeSeries.Records)
                                : timeSeries.Records.ToImmutableList());

                        documents[timeSeries.DocumentId] = (value.doc, timeSeriesData);
                        
                        deletedDocs.Remove(timeSeries.DocumentId);
                    }

                    break;
                case DeleteDocument delete:
                    documents.Remove(delete.Id);
                    
                    if (!deletedDocs.Contains(delete.Id))
                        deletedDocs.Add(delete.Id);

                    break;
            }
        }

        if (documents.Count != 0)
        {
            await using var bulkInsert = documentStore.BulkInsert(insertOptions, cancellationToken);

            foreach (var item in documents)
            {
                await bulkInsert.StoreAsync(item.Value.doc, item.Key);
                
                foreach (var timeSeries in item.Value.timeSeries)
                {
                    using var timeSeriesBatch = bulkInsert.TimeSeriesFor(
                        item.Key,
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

        if (deletedDocs.Count == 0) 
            return request.ToResponse();

        using var session = documentStore.OpenAsyncSession();

        foreach (var item in deletedDocs)
            session.Delete(item);

        await session.SaveChangesAsync(cancellationToken);

        return request.ToResponse();
    }
}