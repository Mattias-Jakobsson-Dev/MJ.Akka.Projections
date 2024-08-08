using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryStringIdProjectionStorageTests : ProjectionStorageTests<string>
{
    protected override string CreateRandomId()
    {
        return Guid.NewGuid().ToString();
    }

    protected override IProjectionStorage GetStorage()
    {
        return new InMemoryPositionStorage();
    }
}