using MJ.Akka.Projections.Cluster.Sharding;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithClusterSingletonCoordinatorAndShardedProjectors(
    ClusteredActorSystemSupplier actorSystemHandler)
    : TestProjectionBaseContinuousTests<string>(actorSystemHandler), IClassFixture<ClusteredActorSystemSupplier>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
    {
        return config
            .AsClusterSingleton()
            .WithSharding();
    }
}