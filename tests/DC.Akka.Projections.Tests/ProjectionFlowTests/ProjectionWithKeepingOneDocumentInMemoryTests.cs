using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithKeepingOneDocumentInMemoryTests : TestProjectionBaseFlowTests<string>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration> config)
    {
        return config
            .WithInProcProjectionFactory()
            .KeepLimitedInMemory(1);
    }
}