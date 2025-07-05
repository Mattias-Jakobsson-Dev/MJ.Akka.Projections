using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InMemory;

public record StoreDocumentInMemory(object Id, object Document) : ICanBePersistedInMemory;

public record DeleteDocumentInMemory(object Id) : ICanBePersistedInMemory;

public interface ICanBePersistedInMemory : ICanBePersisted;

public class InMemoryProjectionStorage(ConcurrentDictionary<object, ReadOnlyMemory<byte>> storage) 
    : IProjectionStorage
{
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
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
                
            storage.AddOrUpdate(document.Key, _ => serialized, (_, _) => serialized);
        }

        foreach (var deletedDocument in deletedDocuments)
        {
            storage.TryRemove(deletedDocument, out _);
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