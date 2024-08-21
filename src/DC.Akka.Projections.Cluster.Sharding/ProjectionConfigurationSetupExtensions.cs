using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Cluster.Sharding;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IConfigurePart<ProjectionSystemConfiguration, ShardedProjectors> WithSharding(
        this IHaveConfiguration<ProjectionSystemConfiguration> setup,
        int maxNumberOfShards = 100,
        Func<ClusterShardingSettings, ClusterShardingSettings>? configureShard = null)
    {
        return setup
            .WithProjectionFactory(new ShardedProjectors(
                setup.ActorSystem,
                (configureShard ?? (x => x))(ClusterShardingSettings.Create(setup.ActorSystem)),
                maxNumberOfShards));
    }

    public static IConfigurePart<ProjectionSystemConfiguration, ClusterSingletonProjectionCoordinator.Setup>
        AsClusterSingleton(
            this IHaveConfiguration<ProjectionSystemConfiguration> source,
            Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
    {
        return source
            .WithCoordinator(new ClusterSingletonProjectionCoordinator.Setup(
                source.ActorSystem,
                (configureCoordinator ?? (x => x))(
                    ClusterSingletonManagerSettings.Create(source.ActorSystem))));
    }
    
    public static IConfigurePart<ProjectionSystemConfiguration, ShardedDaemonProjectionCoordinator.Setup>
        AsShardedDaemon(
            this IHaveConfiguration<ProjectionSystemConfiguration> source,
            string name = "ProjectionsCoordinatorDaemon",
            Func<ShardedDaemonProcessSettings, ShardedDaemonProcessSettings>? configureDaemon = null)
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