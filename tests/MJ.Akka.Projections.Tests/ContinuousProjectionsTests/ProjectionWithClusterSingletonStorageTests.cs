using MJ.Akka.Projections.Cluster.Sharding;
using MJ.Akka.Projections.Configuration;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithClusterSingletonStorageTests(ClusteredActorSystemSupplier actorSystemHandler)
    : TestProjectionBaseContinuousTests<string>(actorSystemHandler), IClassFixture<ClusteredActorSystemSupplier>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration> config)
    {
        return config
            .WithClusterSingletonInMemoryStorage()
            .WithClusterSingletonInMemoryPositionStorage();
    }
}