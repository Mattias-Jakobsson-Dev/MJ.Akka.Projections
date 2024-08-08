using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Akka.Actor;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Storage;

public class InMemoryPositionStorage<TId, TDocument> : IProjectionStorage<TId, TDocument> 
    where TId : notnull where TDocument : notnull
{
    protected readonly ConcurrentDictionary<TId, (Type Type, ReadOnlyMemory<byte> Data)> Documents = new();
    
    public async Task<(TDocument? document, bool requireReload)> LoadDocument(
        TId id,
        CancellationToken cancellationToken = default)
    {
        if (!Documents.TryGetValue(id, out var data))
            return (default, true);

        return ((TDocument?)await DeserializeData(data.Data, data.Type), true);
    }

    public Task<IStorageTransaction> StartTransaction(
        IImmutableList<(TId Id, TDocument Document, IActorRef ackTo)> toUpsert,
        IImmutableList<(TId id, IActorRef ackTo)> toDelete,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IStorageTransaction>(new InMemoryProjectionStorageTransaction<TId, TDocument>(
            toUpsert,
            toDelete,
            Documents,
            SerializeData));
    }

    [PublicAPI]
    protected static async Task<ReadOnlyMemory<byte>> SerializeData(object data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        await using var writer = new Utf8JsonWriter(buffer);

        JsonSerializer.Serialize(writer, data);

        return buffer.WrittenMemory;
    }

    protected static Task<object?> DeserializeData(ReadOnlyMemory<byte> data, Type? type)
    {
        return Task.FromResult(type == null ? null : JsonSerializer.Deserialize(data.Span, type));
    }
}