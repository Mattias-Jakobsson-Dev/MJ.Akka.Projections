using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Cluster.Sharding;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.ContinuousProjectionsTests;
using Hyperion.Internal;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class ClusterSingletonProjectionStorageTests(ClusteredActorSystemSupplier actorSystemSupplier) 
    : TestDocumentProjectionStorageTests<string>, IClassFixture<ClusteredActorSystemSupplier>
{
    protected override IProjectionStorage GetStorage()
    {
        var actorSystem = actorSystemSupplier.StartNewActorSystem();
        
        return ClusterSingletonProjectionStorage.Create(
            actorSystem,
            "projection-storage",
            ClusterSingletonManagerSettings.Create(actorSystem));
    }
}