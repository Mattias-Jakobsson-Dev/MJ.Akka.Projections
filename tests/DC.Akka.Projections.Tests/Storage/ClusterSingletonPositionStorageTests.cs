using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Cluster.Sharding;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.ContinuousProjectionsTests;
using Hyperion.Internal;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class ClusterSingletonPositionStorageTests(ClusteredActorSystemSupplier actorSystemSupplier)  
    : PositionStorageTests, IClassFixture<ClusteredActorSystemSupplier>
{
    protected override IProjectionPositionStorage GetStorage()
    {
        var actorSystem = actorSystemSupplier.StartNewActorSystem();
        
        return ClusterSingletonPositionStorage.Create(
            actorSystem,
            "position-storage",
            ClusterSingletonManagerSettings.Create(actorSystem));
    }
}