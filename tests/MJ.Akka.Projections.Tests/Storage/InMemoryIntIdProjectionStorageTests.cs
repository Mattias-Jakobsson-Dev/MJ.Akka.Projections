using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryIntIdProjectionStorageTests : TestDocumentProjectionStorageTests<int>
{
    protected override IProjectionStorage GetStorage()
    {
        return new InMemoryProjectionStorage();
    }
}