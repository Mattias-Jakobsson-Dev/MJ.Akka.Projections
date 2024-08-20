using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Storage.RavenDb;
using DC.Akka.Projections.Tests.Storage;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithRavenDbStorageTests(RavenDbFixture fixture) 
    : TestProjectionBaseFlowTests<string>, IClassFixture<RavenDbFixture>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();

    protected override IProjectionStorage GetProjectionStorage()
    {
        return new RavenDbProjectionStorage(_documentStore, new BulkInsertOptions());
    }

    protected override IProjectionPositionStorage GetPositionStorage()
    {
        return new RavenDbProjectionPositionStorage(_documentStore);
    }
}