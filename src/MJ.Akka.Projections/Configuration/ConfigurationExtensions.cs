using Akka.Streams;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurePart<T, RestartSettings?> WithRestartSettings<T>(
        this IHaveConfiguration<T> source,
        RestartSettings? restartSettings)
        where T : ProjectionConfig
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            RestartSettings = restartSettings
        });

        return new ConfigurePart<T, RestartSettings?>(config, restartSettings);
    }

    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, TFactory> WithProjectionFactory<TFactory,
        TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> source,
        TFactory factory)
        where TFactory : IKeepTrackOfProjectors where TStorageSetup : IStorageSetup
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            ProjectorFactory = factory
        });

        return new ConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, TFactory>(config, factory);
    }

    public static IConfigurePart<T, TStrategy> WithEventBatchingStrategy<T, TStrategy>(
        this IHaveConfiguration<T> source,
        TStrategy strategy)
        where TStrategy : IEventBatchingStrategy
        where T : ContinuousProjectionConfig
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            EventBatchingStrategy = strategy
        });

        return new ConfigurePart<T, TStrategy>(config, strategy);
    }

    public static IConfigurePart<T, TStrategy> WithPositionStorageBatchingStrategy<T, TStrategy>(
        this IHaveConfiguration<T> source,
        TStrategy strategy)
        where TStrategy : IEventPositionBatchingStrategy
        where T : ContinuousProjectionConfig
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            PositionBatchingStrategy = strategy
        });

        return new ConfigurePart<T, TStrategy>(config, strategy);
    }

    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, TCoordinator> WithCoordinator<
        TCoordinator, TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> source,
        TCoordinator coordinator)
        where TCoordinator : IConfigureProjectionCoordinator where TStorageSetup : IStorageSetup
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            Coordinator = coordinator
        });

        return new ConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, TCoordinator>(config, coordinator);
    }

    internal static IConfigureProjectionCoordinator Build<TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> conf)
        where TStorageSetup : IStorageSetup
    {
        foreach (var projection in conf.Config.Projections)
        {
            var configuration = projection.Value(conf.Config);

            conf.Config.Coordinator.WithProjection(configuration);
        }

        return conf.Config.Coordinator;
    }
}