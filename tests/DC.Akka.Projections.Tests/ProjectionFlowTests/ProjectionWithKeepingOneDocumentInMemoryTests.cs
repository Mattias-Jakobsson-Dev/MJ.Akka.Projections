using DC.Akka.Projections.Configuration;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithKeepingOneDocumentInMemoryTests(NormalTestKitActorSystem actorSystemSetup) 
    : TestProjectionBaseFlowTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration> config)
    {
        return config
            .WithInProcProjectionFactory()
            .KeepLimitedInMemory(1);
    }
}