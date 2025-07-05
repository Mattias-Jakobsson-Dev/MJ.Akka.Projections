using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Cluster.Sharding;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, ShardedProjectors>
        WithSharding<TStorageSetup>(
            this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> setup,
            int maxNumberOfShards = 100,
            Func<ClusterShardingSettings, ClusterShardingSettings>? configureShard = null)
        where TStorageSetup : IStorageSetup
    {
        return setup
            .WithProjectionFactory(new ShardedProjectors(
                setup.ActorSystem,
                (configureShard ?? (x => x))(ClusterShardingSettings.Create(setup.ActorSystem)),
                maxNumberOfShards,
                Guid.NewGuid().ToString()));
    }

    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>,
            ClusterSingletonProjectionCoordinator.Setup>
        AsClusterSingleton<TStorageSetup>(
            this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> source,
            Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
        where TStorageSetup : IStorageSetup
    {
        return source
            .WithCoordinator(new ClusterSingletonProjectionCoordinator.Setup(
                source.ActorSystem,
                (configureCoordinator ?? (x => x))(
                    ClusterSingletonManagerSettings.Create(source.ActorSystem))));
    }

    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, ShardedDaemonProjectionCoordinator.Setup>
        AsShardedDaemon<TStorageSetup>(
            this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> source,
            string name = "ProjectionsCoordinatorDaemon",
            Func<ShardedDaemonProcessSettings, ShardedDaemonProcessSettings>? configureDaemon = null)
        where TStorageSetup : IStorageSetup
    {
        return source
            .WithCoordinator(new ShardedDaemonProjectionCoordinator.Setup(
                source.ActorSystem,
                name,
                (configureDaemon ?? (x => x))(
                    ShardedDaemonProcessSettings.Create(source.ActorSystem)
                        .WithShardingSettings(ClusterShardingSettings.Create(source.ActorSystem)))));
    }
}