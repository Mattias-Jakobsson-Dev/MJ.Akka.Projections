using System.Collections.Immutable;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests.TestData;

public class TestInMemoryProjectionStorage : InMemoryProjectionStorage
{
    public async Task<IImmutableList<object>> LoadAll()
    {
        return (await Task.WhenAll(Documents.Select(async x => await DeserializeData(x.Value.Data, x.Value.Type))))
            .Where(x => x != null)
            .Select(x => x!)
            .ToImmutableList();
    }
}