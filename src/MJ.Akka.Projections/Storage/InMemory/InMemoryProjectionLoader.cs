using System.Text.Json;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionLoader<TId, TDocument>(Func<TId, ReadOnlyMemory<byte>?> loadDocument)
    : ILoadProjectionContext<TId, InMemoryProjectionContext<TId, TDocument>>
    where TId : notnull where TDocument : class
{
    public Task<InMemoryProjectionContext<TId, TDocument>> Load(
        TId id,
        Func<TId, InMemoryProjectionContext<TId, TDocument>> getDefaultContext,
        CancellationToken cancellationToken = default)
    {
        var data = loadDocument(id);

        if (data == null)
            return Task.FromResult(getDefaultContext(id));
        
        var document = DeserializeData(data.Value);

        return Task.FromResult(
            new InMemoryProjectionContext<TId, TDocument>(id, document));
    }
    
    private static TDocument? DeserializeData(ReadOnlyMemory<byte> data)
    {
        return JsonSerializer.Deserialize<TDocument>(data.Span);
    }
}