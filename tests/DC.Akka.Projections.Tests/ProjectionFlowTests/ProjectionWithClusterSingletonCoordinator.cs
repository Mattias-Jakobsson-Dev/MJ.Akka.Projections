using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Cluster.Sharding;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithClusterSingletonCoordinator(ClusteredActorSystemSupplier actorSystemHandler)
    : TestProjectionBaseFlowTests<string>(actorSystemHandler), IClassFixture<ClusteredActorSystemSupplier>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration> config)
    {
        return config
            .AsClusterSingleton();
    }
}