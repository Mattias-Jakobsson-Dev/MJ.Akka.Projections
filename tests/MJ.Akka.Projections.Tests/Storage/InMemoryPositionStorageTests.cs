using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryPositionStorageTests : PositionStorageTests
{
    protected override IProjectionPositionStorage GetStorage()
    {
        return new InMemoryPositionStorage();
    }
}