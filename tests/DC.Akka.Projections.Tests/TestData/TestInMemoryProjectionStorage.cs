using System.Collections.Immutable;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Tests.TestData;

public class TestInMemoryProjectionStorage<TDocument> : InMemoryProjectionStorage<string, TDocument>
    where TDocument : notnull
{
    public async Task<IImmutableList<object>> LoadAll()
    {
        return (await Task.WhenAll(Documents.Select(async x => await DeserializeData(x.Value.Data, x.Value.Type))))
            .Where(x => x != null)
            .Select(x => x!)
            .ToImmutableList();
    }
}