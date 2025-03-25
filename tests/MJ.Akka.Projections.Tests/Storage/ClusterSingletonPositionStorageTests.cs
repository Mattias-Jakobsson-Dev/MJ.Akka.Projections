using Akka.Cluster.Tools.Singleton;
using MJ.Akka.Projections.Cluster.Sharding;
using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

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