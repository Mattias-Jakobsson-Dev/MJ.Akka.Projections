using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryStringIdProjectionStorageTests : TestDocumentProjectionStorageTests<string>
{
    protected override IProjectionStorage GetStorage()
    {
        return new InMemoryProjectionStorage();
    }
}