using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbPositionStorageTests(RavenDbFixture fixture) : PositionStorageTests, IClassFixture<RavenDbFixture>
{
    protected override IProjectionPositionStorage GetStorage()
    {
        return new RavenDbProjectionPositionStorage(fixture.OpenDocumentStore());
    }
}