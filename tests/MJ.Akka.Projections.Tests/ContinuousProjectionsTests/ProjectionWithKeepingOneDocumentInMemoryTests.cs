using MJ.Akka.Projections.Configuration;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithKeepingOneDocumentInMemoryTests(NormalTestKitActorSystem actorSystemSetup) 
    : TestProjectionBaseContinuousTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration> config)
    {
        return config
            .WithInProcProjectionFactory()
            .KeepLimitedInMemory(1);
    }

    protected override TimeSpan ProjectionWaitTime { get; } = TimeSpan.FromSeconds(20);
}