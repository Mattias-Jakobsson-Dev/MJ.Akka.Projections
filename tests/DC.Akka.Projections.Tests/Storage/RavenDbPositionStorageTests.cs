using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Storage.RavenDb;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbPositionStorageTests(RavenDbFixture fixture) : PositionStorageTests, IClassFixture<RavenDbFixture>
{
    protected override IProjectionPositionStorage GetStorage()
    {
        return new RavenDbProjectionPositionStorage(fixture.OpenDocumentStore());
    }
}