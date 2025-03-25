using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryIntIdProjectionStorageTests : TestDocumentProjectionStorageTests<int>
{
    protected override IProjectionStorage GetStorage()
    {
        return new InMemoryProjectionStorage();
    }
}