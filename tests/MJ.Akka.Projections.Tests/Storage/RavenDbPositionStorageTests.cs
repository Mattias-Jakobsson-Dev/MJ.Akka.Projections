using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using JetBrains.Annotations;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbPositionStorageTests(RavenDbFixture fixture) : PositionStorageTests, IClassFixture<RavenDbFixture>
{
    protected override Task<IProjectionPositionStorage> GetStorage()
    {
        return Task.FromResult<IProjectionPositionStorage>(
            new RavenDbProjectionPositionStorage(fixture.OpenDocumentStore()));
    }
}