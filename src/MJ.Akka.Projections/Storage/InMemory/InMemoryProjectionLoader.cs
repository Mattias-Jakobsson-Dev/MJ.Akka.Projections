using System.Text.Json;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionLoader<TId, TDocument>(
    Func<TId, ReadOnlyMemory<byte>?> loadDocument,
    Func<TId, TDocument?> getDefaultDocument)
    : ILoadProjectionContext<TId, InMemoryProjectionContext<TId, TDocument>>
    where TId : notnull where TDocument : class
{
    public Task<InMemoryProjectionContext<TId, TDocument>> Load(
        TId id,
        CancellationToken cancellationToken = default)
    {
        var data = loadDocument(id);

        var document = data != null ? DeserializeData(data.Value) : getDefaultDocument(id);

        return Task.FromResult(
            new InMemoryProjectionContext<TId, TDocument>(id, document));
    }
    
    private static TDocument? DeserializeData(ReadOnlyMemory<byte> data)
    {
        return JsonSerializer.Deserialize<TDocument>(data.Span);
    }
}