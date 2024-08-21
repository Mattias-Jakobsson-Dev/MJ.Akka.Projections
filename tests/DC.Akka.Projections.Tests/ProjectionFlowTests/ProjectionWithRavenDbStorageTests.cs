using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage.RavenDb;
using DC.Akka.Projections.Tests.Storage;
using Raven.Client.Documents;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithRavenDbStorageTests(RavenDbFixture fixture, NormalTestKitActorSystem actorSystemSetup)
    : TestProjectionBaseFlowTests<string>(actorSystemSetup), IClassFixture<RavenDbFixture>,
        IClassFixture<NormalTestKitActorSystem>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();

    protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration> config)
    {
        return config
            .WithRavenDbDocumentStorage(_documentStore)
            .WithRavenDbPositionStorage(_documentStore);
    }
}