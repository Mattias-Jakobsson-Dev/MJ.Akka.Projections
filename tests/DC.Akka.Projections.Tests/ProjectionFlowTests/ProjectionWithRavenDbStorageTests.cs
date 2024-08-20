using Akka.Actor;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Storage.RavenDb;
using DC.Akka.Projections.Tests.Storage;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithRavenDbStorageTests(RavenDbFixture fixture, NormalTestKitActorSystem actorSystemSetup)
    : TestProjectionBaseFlowTests<string>(actorSystemSetup), IClassFixture<RavenDbFixture>,
        IClassFixture<NormalTestKitActorSystem>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();

    protected override IProjectionStorage GetProjectionStorage(ActorSystem system)
    {
        return new RavenDbProjectionStorage(_documentStore, new BulkInsertOptions());
    }

    protected override IProjectionPositionStorage GetPositionStorage(ActorSystem system)
    {
        return new RavenDbProjectionPositionStorage(_documentStore);
    }
}