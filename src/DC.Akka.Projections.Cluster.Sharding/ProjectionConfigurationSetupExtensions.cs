﻿using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Cluster.Sharding;

[PublicAPI]
public static class ProjectionConfigurationSetupExtensions
{
    public static IProjectionPartSetup<T> WithSharding<T>(
        this IProjectionPartSetup<T> setup,
        int maxNumberOfShards = 100,
        Func<ClusterShardingSettings, ClusterShardingSettings>? configureShard = null,
        Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
        where T : IProjectionPartSetup<T>
    {
        return setup
            .AsClusterSingleton(configureCoordinator)
            .WithProjectionFactory(new ShardedProjectors(
                setup.ActorSystem,
                (configureShard ?? (x => x))(ClusterShardingSettings.Create(setup.ActorSystem)),
                maxNumberOfShards));
    }

    public static IProjectionPartSetup<T> AsClusterSingleton<T>(
        this IProjectionPartSetup<T> setup,
        Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
        where T : IProjectionPartSetup<T>
    {
        return setup
            .WithCoordinatorFactory(new ClusterSingletonProjectionCoordinator(
                setup.ActorSystem,
                (configureCoordinator ?? (x => x))(
                    ClusterSingletonManagerSettings.Create(setup.ActorSystem))));
    }
}