using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Storage.RavenDb;
using JetBrains.Annotations;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbProjectionStorageTests(RavenDbFixture fixture) 
    : TestDocumentProjectionStorageTests<string>, IClassFixture<RavenDbFixture>
{
    protected override IProjectionStorage GetStorage()
    {
        return new RavenDbProjectionStorage(fixture.OpenDocumentStore(), new BulkInsertOptions());
    }
}