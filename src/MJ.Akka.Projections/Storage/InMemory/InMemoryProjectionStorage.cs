using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionStorage(ConcurrentDictionary<object, ReadOnlyMemory<byte>> storage) 
    : IProjectionStorage
{
    private readonly InProcessProjector<InMemoryStorageProjectorResult> _storageProjector = DocumentsStorageProjector
        .Setup<InMemoryStorageProjectorResult>(_ => { });
    
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (unhandledEvents, results) = _storageProjector
            .RunFor(
                request.Results.OfType<object>().ToImmutableList(),
                InMemoryStorageProjectorResult.Empty);
        
        foreach (var item in results.DocumentsToUpsert)
        {
            var serialized = await SerializeData(item.Value);
                
            storage.AddOrUpdate(item.Key, _ => serialized, (_, _) => serialized);
        }

        foreach (var item in results.DocumentsToDelete)
        {
            storage.TryRemove(item, out _);
        }

        return StoreProjectionResponse.From(unhandledEvents);
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