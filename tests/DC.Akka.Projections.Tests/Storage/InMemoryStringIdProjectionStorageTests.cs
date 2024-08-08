using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryStringIdProjectionStorageTests : ProjectionStorageTests<string>
{
    protected override string CreateRandomId()
    {
        return Guid.NewGuid().ToString();
    }

    protected override IProjectionStorage<string, TestDocument<string>> GetStorage()
    {
        return new InMemoryPositionStorage<string, TestDocument<string>>();
    }
}