using System.Collections.Immutable;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurePart<T, TStorage> WithProjectionStorage<T, TStorage>(
        this IHaveConfiguration<T> source,
        TStorage storage) where TStorage : IProjectionStorage
        where T : ContinuousProjectionConfig
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            ProjectionStorage = storage
        });

        return new ConfigurePart<T, TStorage>(config, storage);
    }

    public static IConfigurePart<T, TStorage> WithPositionStorage<T, TStorage>(
        this IHaveConfiguration<T> source,
        TStorage storage)
        where TStorage : IProjectionPositionStorage
        where T : ContinuousProjectionConfig
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            PositionStorage = storage
        });

        return new ConfigurePart<T, TStorage>(config, storage);
    }

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
    
    public static IConfigurePart<ProjectionSystemConfiguration, TFactory> WithProjectionFactory<TFactory>(
        this IHaveConfiguration<ProjectionSystemConfiguration> source,
        TFactory factory) 
        where TFactory : IKeepTrackOfProjectors
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            ProjectorFactory = factory
        });

        return new ConfigurePart<ProjectionSystemConfiguration, TFactory>(config, factory);
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
    
    public static IConfigurePart<ProjectionSystemConfiguration, TCoordinator> WithCoordinator<TCoordinator>(
        this IHaveConfiguration<ProjectionSystemConfiguration> source,
        TCoordinator coordinator) 
        where TCoordinator : IConfigureProjectionCoordinator
    {
        var config = source.WithModifiedConfig(conf => conf with
        {
            Coordinator = coordinator
        });

        return new ConfigurePart<ProjectionSystemConfiguration, TCoordinator>(config, coordinator);
    }

    internal static IConfigureProjectionCoordinator Build(
        this IHaveConfiguration<ProjectionSystemConfiguration> conf)
    {
        var result = new Dictionary<string, ProjectionConfiguration>();

        foreach (var projection in conf.Config.Projections)
        {
            var configuration = projection.Value(conf.Config);

            conf.Config.Coordinator.WithProjection(configuration.GetProjection());

            result[projection.Key] = configuration;
        }

        ProjectionConfigurationsSupplier.Register(conf.ActorSystem, result.ToImmutableDictionary());

        return conf.Config.Coordinator;
    }
}