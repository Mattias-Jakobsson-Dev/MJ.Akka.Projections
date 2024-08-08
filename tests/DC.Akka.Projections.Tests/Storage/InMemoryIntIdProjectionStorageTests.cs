using DC.Akka.Projections.Storage;
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

    protected override IProjectionStorage GetStorage()
    {
        return new InMemoryPositionStorage();
    }
}