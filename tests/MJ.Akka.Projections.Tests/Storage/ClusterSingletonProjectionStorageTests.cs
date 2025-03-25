using Akka.Cluster.Tools.Singleton;
using MJ.Akka.Projections.Cluster.Sharding;
using MJ.Akka.Projections.Storage;
using JetBrains.Annotations;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

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