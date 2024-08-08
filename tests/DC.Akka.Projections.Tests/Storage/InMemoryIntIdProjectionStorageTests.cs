using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryIntIdProjectionStorageTests : ProjectionStorageTests<int>
{
    private readonly Random _random = new();
    
    protected override int CreateRandomId()
    {
        return _random.Next(int.MaxValue);
    }

    protected override IProjectionStorage<int, TestDocument<int>> GetStorage()
    {
        return new InMemoryPositionStorage<int, TestDocument<int>>();
    }
}