using System.Text.Json;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionLoader<TIdContext, TDocument>(Func<TIdContext, ReadOnlyMemory<byte>?> loadDocument)
    : ILoadProjectionContext<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>>
    where TIdContext : IProjectionIdContext where TDocument : class
{
    public Task<InMemoryProjectionContext<TIdContext, TDocument>> Load(
        TIdContext id,
        Func<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>> getDefaultContext,
        CancellationToken cancellationToken = default)
    {
        var data = loadDocument(id);

        if (data == null)
            return Task.FromResult(getDefaultContext(id));
        
        var document = DeserializeData(data.Value);

        return Task.FromResult(
            new InMemoryProjectionContext<TIdContext, TDocument>(id, document));
    }
    
    private static TDocument? DeserializeData(ReadOnlyMemory<byte> data)
    {
        return JsonSerializer.Deserialize<TDocument>(data.Span);
    }
}