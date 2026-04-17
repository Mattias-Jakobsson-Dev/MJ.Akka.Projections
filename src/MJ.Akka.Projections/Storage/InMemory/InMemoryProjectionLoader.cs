using System.Text.Json;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionLoader<TId, TDocument>(Func<SimpleIdContext<TId>, ReadOnlyMemory<byte>?> loadDocument)
    : ILoadProjectionContext<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>>
    where TDocument : class
{
    public Task<InMemoryProjectionContext<TId, TDocument>> Load(
        SimpleIdContext<TId> id,
        Func<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>> getDefaultContext,
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