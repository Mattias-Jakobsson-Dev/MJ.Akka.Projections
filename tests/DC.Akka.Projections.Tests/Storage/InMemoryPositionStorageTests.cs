using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryPositionStorageTests : PositionStorageTests
{
    protected override IProjectionPositionStorage GetStorage()
    {
        return new InMemoryProjectionPositionStorage();
    }
}