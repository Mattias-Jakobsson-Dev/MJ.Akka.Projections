using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage;

public record StoreDocumentInMemory(object Id, object Document) : ICanBePersistedInMemory;

public record DeleteDocumentInMemory(object Id) : ICanBePersistedInMemory;

public interface ICanBePersistedInMemory : ICanBePersisted;

public class InMemoryProjectionContext<TId, TDocument>(TId id, TDocument? document) 
    : ProjectedDocumentContext<TId, TDocument>(id, document)
    where TId : notnull where TDocument : class
{
    private readonly bool _existed = document != null;
    
    public override PrepareForStorageResponse PrepareForStorage()
    {
        var results = ImmutableList<ICanBePersisted>.Empty;

        if (Exists())
            results = results.Add(new StoreDocumentInMemory(Id, Document));
        else if (_existed && !Exists())
            results = results.Add(new DeleteDocumentInMemory(Id));

        return new PrepareForStorageResponse(
            Id,
            results,
            new InMemoryProjectionContext<TId, TDocument>(Id, Document));
    }
}

public class InMemoryProjectionLoader<TId, TDocument>(Func<TId, ReadOnlyMemory<byte>?> loadDocument)
    : ILoadProjectionContext<TId, InMemoryProjectionContext<TId, TDocument>>
    where TId : notnull where TDocument : class
{
    public Task<InMemoryProjectionContext<TId, TDocument>> Load(
        TId id,
        CancellationToken cancellationToken = default)
    {
        var data = loadDocument(id);

        return Task.FromResult(
            new InMemoryProjectionContext<TId, TDocument>(id, data != null ? DeserializeData(data.Value) : null));
    }
    
    private static TDocument? DeserializeData(ReadOnlyMemory<byte> data)
    {
        return JsonSerializer.Deserialize<TDocument>(data.Span);
    }
}

public class InMemoryProjectionStorage : IProjectionStorage
{
    protected readonly ConcurrentDictionary<object, ReadOnlyMemory<byte>> Documents = new();
    
    public InMemoryProjectionLoader<TId, TDocument> CreateLoader<TId, TDocument>()
        where TId : notnull where TDocument : class
    {
        return new InMemoryProjectionLoader<TId, TDocument>(id =>
            Documents.TryGetValue(id, out var data) ? data : null);
    }

    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var documents = new Dictionary<object, object>();
        var deletedDocuments = new HashSet<object>();
        
        (request, var items) = request.Take<ICanBePersistedInMemory>();

        foreach (var item in items)
        {
            if (item is StoreDocumentInMemory store)
            {
                deletedDocuments.Remove(store.Id);
                
                documents[store.Id] = store.Document;
            }
            else if (item is DeleteDocumentInMemory delete)
            {
                deletedDocuments.Add(delete.Id);
                
                documents.Remove(delete.Id);
            }
        }

        foreach (var document in documents)
        {
            var serialized = await SerializeData(document.Value);
                
            Documents.AddOrUpdate(document.Key, _ => serialized, (_, _) => serialized);
        }

        foreach (var deletedDocument in deletedDocuments)
        {
            Documents.TryRemove(deletedDocument, out _);
        }

        return request.ToResponse();
    }
    
    [PublicAPI]
    protected static async Task<ReadOnlyMemory<byte>> SerializeData(object data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        await using var writer = new Utf8JsonWriter(buffer);

        JsonSerializer.Serialize(writer, data);

        return buffer.WrittenMemory;
    }
}