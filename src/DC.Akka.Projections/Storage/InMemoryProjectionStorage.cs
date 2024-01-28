using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;

namespace DC.Akka.Projections.Storage;

public class InMemoryProjectionStorage : IProjectionStorage
{
    protected readonly ConcurrentDictionary<object, (Type Type, ReadOnlyMemory<byte> Data)> Documents = new();
    protected readonly ConcurrentDictionary<string, long> Positions = new();
    
    public async Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default)
    {
        if (!Documents.TryGetValue(id, out var data))
            return default;

        return (TDocument?)await DeserializeData(data.Data, data.Type);
    }

    public async Task StoreDocuments(
        IImmutableList<(ProjectedDocument Document, Action Ack, Action<Exception?> Nack)> documents, 
        CancellationToken cancellationToken = default)
    {
        foreach (var document in documents.Where(x => x.Document.Document != null))
        {
            var serialized = await SerializeData(document.Document.Document!);

            var result = (document.Document.Document!.GetType(), serialized);
            
            Documents.AddOrUpdate(document.Document.Id, _ => result, (_, _) => result);
            
            document.Ack();
        }

        foreach (var document in documents.Where(x => x.Document.Document == null))
        {
            Documents.TryRemove(document.Document.Id, out _);

            document.Ack();
        }
    }

    public Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
    {
        return Positions.TryGetValue(projectionName, out var position)
            ? Task.FromResult<long?>(position)
            : Task.FromResult<long?>(default);
    }

    public Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        if (position != null)
            Positions.AddOrUpdate(projectionName, _ => position.Value, (_, _) => position.Value);

        return LoadLatestPosition(projectionName, cancellationToken);
    }

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