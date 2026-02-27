using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionStorage(ConcurrentDictionary<ProjectionContextId, ReadOnlyMemory<byte>> storage) 
    : IProjectionStorage
{
    public async Task Store(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invalidDocuments = contexts
            .Where(x => x.Value is not IInMemoryProjectionContext)
            .Select(x => x.Key)
            .ToImmutableList();

        if (!invalidDocuments.IsEmpty)
            throw new InvalidContextStorageException(invalidDocuments);

        var documents = contexts
            .Where(x => x.Value is IInMemoryProjectionContext)
            .Select(x => new
            {
                Id = x.Key,
                ((IInMemoryProjectionContext)x.Value).Document
            })
            .ToImmutableList();
        
        var documentsToUpsert = documents
            .Where(x => x.Document != null)
            .ToImmutableDictionary(x => x.Id, x => x.Document!);
        
        var documentsToDelete = documents
            .Where(x => x.Document == null)
            .Select(x => x.Id)
            .ToImmutableList();
        
        foreach (var item in documentsToUpsert)
        {
            var serialized = await SerializeData(item.Value);
                
            storage.AddOrUpdate(item.Key, _ => serialized, (_, _) => serialized);
        }

        foreach (var item in documentsToDelete)
        {
            storage.TryRemove(item, out _);
        }
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