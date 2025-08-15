using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryPositionStorageTests : PositionStorageTests
{
    protected override Task<IProjectionPositionStorage> GetStorage()
    {
        return Task.FromResult<IProjectionPositionStorage>(new InMemoryPositionStorage());
    }
}