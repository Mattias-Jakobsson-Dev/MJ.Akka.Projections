using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Cluster.Sharding;
using DC.Akka.Projections.Storage;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithClusterSingletonStorageTests(ClusteredActorSystemSupplier actorSystemHandler)
    : TestProjectionBaseFlowTests<string>(actorSystemHandler), IClassFixture<ClusteredActorSystemSupplier>
{
    protected override IProjectionStorage GetProjectionStorage(ActorSystem system)
    {
        return ClusterSingletonProjectionStorage.Create(
            system,
            "projection-storage",
            ClusterSingletonManagerSettings.Create(system));
    }

    protected override IProjectionPositionStorage GetPositionStorage(ActorSystem system)
    {
        return ClusterSingletonPositionStorage.Create(
            system,
            "position-storage",
            ClusterSingletonManagerSettings.Create(system));
    }
}