using JetBrains.Annotations;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbStashStorageTests(RavenDbFixture fixture) : StashStorageTests, IClassFixture<RavenDbFixture>
{
    protected override IProjectionStashStorage GetStorage() =>
        new RavenDbProjectionStashStorage(fixture.OpenDocumentStore());

    protected override IProjectionStashStorage GetStorageWithTimeout(TimeSpan timeout) =>
        new RavenDbProjectionStashStorage(fixture.OpenDocumentStore(), inProcessTimeout: timeout);
}

