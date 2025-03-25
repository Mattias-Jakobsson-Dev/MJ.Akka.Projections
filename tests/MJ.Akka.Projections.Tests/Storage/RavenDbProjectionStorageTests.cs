using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbProjectionStorageTests(RavenDbFixture fixture) 
    : TestDocumentProjectionStorageTests<string>, IClassFixture<RavenDbFixture>
{
    protected override IProjectionStorage GetStorage()
    {
        return new RavenDbProjectionStorage(fixture.OpenDocumentStore(), new BulkInsertOptions());
    }
}