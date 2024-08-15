using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

internal record ProjectionsSetup(
    ActorSystem ActorSystem,
    IStartProjectionCoordinator CoordinatorFactory,
    IKeepTrackOfProjectors ProjectionFactory,
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration ProjectionStreamConfiguration,
    IProjectionStorage Storage,
    IProjectionPositionStorage PositionStorage,
    IImmutableDictionary<string, Func<IProjectionsSetup, Task<IProjectionConfiguration>>> ProjectionSetups) 
    : IProjectionsSetup, IProjectionStoragePartSetup<IProjectionsSetup>
{
    public IProjectionsSetup WithCoordinatorFactory(IStartProjectionCoordinator factory)
    {
        return this with
        {
            CoordinatorFactory = factory
        };
    }

    public IProjectionsSetup WithProjectionFactory(IKeepTrackOfProjectors factory)
    {
        return this with
        {
            ProjectionFactory = factory
        };
    }

    public IProjectionsSetup WithRestartSettings(RestartSettings restartSettings)
    {
        return this with
        {
            RestartSettings = restartSettings
        };
    }

    public IProjectionsSetup WithProjectionStreamConfiguration(
        ProjectionStreamConfiguration projectionStreamConfiguration)
    {
        return this with
        {
            ProjectionStreamConfiguration = projectionStreamConfiguration
        };
    }

    public IProjectionStoragePartSetup<IProjectionsSetup> WithProjectionStorage(IProjectionStorage storage)
    {
        return this with
        {
            Storage = storage
        };
    }
    
    public IProjectionStoragePartSetup<IProjectionsSetup> Batched(int batchSize = 100, int parallelism = 5)
    {
        return this with
        {
            Storage = Storage.Batched(ActorSystem, batchSize, parallelism)
        };
    }

    public IProjectionsSetup Config => this;

    public IProjectionsSetup WithPositionStorage(IProjectionPositionStorage positionStorage)
    {
        return this with
        {
            PositionStorage = positionStorage
        };
    }

    public static ProjectionsSetup CreateDefault(ActorSystem actorSystem)
    {
        return new ProjectionsSetup(
            actorSystem,
            new InProcessSingletonProjectionCoordinator(actorSystem),
            new KeepTrackOfProjectorsInProc(actorSystem),
            null,
            ProjectionStreamConfiguration.Default,
            new InMemoryProjectionStorage(),
            new InMemoryPositionStorage(),
            ImmutableDictionary<string, Func<IProjectionsSetup, Task<IProjectionConfiguration>>>.Empty);
    }

    public IProjectionsSetup WithProjection<TId, TDocument>(
        IProjection<TId, TDocument> projection,
        Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>>? configure = null) 
        where TId : notnull where TDocument : notnull
    {
        return this with
        {
            ProjectionSetups = ProjectionSetups
                .SetItem(
                    projection.Name,
                    async x =>
                    {
                        var configuration = (configure ?? (y => y))(new ProjectionConfigurationSetup<TId, TDocument>(
                                projection, 
                                ActorSystem))
                            .Build(x);

                        ActorSystem.RegisterExtension(configuration);
                        
                        await configuration.ProjectionCoordinatorStarter.IncludeProjection(configuration);

                        return configuration;
                    })
        };
    }

    public async Task<ProjectionsApplication> Start()
    {
        var results = new Dictionary<string, IProjectionProxy>();

        foreach (var projectionSetup in ProjectionSetups)
        {
            var configuration = await projectionSetup.Value(this);

            await configuration.ProjectionCoordinatorStarter.Start();

            var proxy = await configuration.ProjectionCoordinatorStarter.GetCoordinatorFor(
                configuration.Projection);
            
            if (proxy != null)
                results[projectionSetup.Key] = proxy;
        }

        var application = new ProjectionsApplication(results.ToImmutableDictionary());

        ActorSystem.RegisterExtension(application);

        return application;
    }
}