using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

internal record ProjectionsSetup(
    ActorSystem ActorSystem,
    Func<Task<IActorRef>>? CoordinatorFactory,
    Func<object, Task<IActorRef>>? ProjectionFactory,
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration ProjectionStreamConfiguration,
    IProjectionStorage Storage,
    IProjectionPositionStorage PositionStorage,
    IImmutableDictionary<string, Func<IProjectionsSetup, Task<IProjectionProxy>>> ProjectionSetups) : IProjectionsSetup
{
    public IProjectionsSetup WithCoordinatorFactory(Func<Task<IActorRef>> factory)
    {
        return this with
        {
            CoordinatorFactory = factory
        };
    }

    public IProjectionsSetup WithProjectionFactory(Func<object, Task<IActorRef>> factory)
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

    public IProjectionsSetup WithProjectionStorage(IProjectionStorage storage)
    {
        return this with
        {
            Storage = storage
        };
    }

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
            null,
            null,
            null,
            ProjectionStreamConfiguration.Default,
            new InMemoryProjectionStorage(),
            new InMemoryPositionStorage(),
            ImmutableDictionary<string, Func<IProjectionsSetup, Task<IProjectionProxy>>>.Empty);
    }

    public IProjectionsSetup WithProjection<TId, TDocument>(
        IProjection<TId, TDocument> projection,
        Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>> configure) 
        where TId : notnull where TDocument : notnull
    {
        return this with
        {
            ProjectionSetups = ProjectionSetups
                .SetItem(
                    projection.Name,
                    async x =>
                    {
                        var configuration = configure(new ProjectionConfigurationSetup<TId, TDocument>(
                                projection, 
                                ActorSystem))
                            .Build(x);

                        ActorSystem.RegisterExtension(configuration);
                        
                        var coordinator = await configuration.CreateProjectionCoordinator();
                        
                        var proxy = new ProjectionsCoordinator<TId, TDocument>.Proxy(coordinator);
                        
                        proxy.Start();

                        return proxy;
                    })
        };
    }

    public async Task<ProjectionsApplication> Start()
    {
        var results = new Dictionary<string, IProjectionProxy>();

        foreach (var projectionSetup in ProjectionSetups)
        {
            results[projectionSetup.Key] = await projectionSetup.Value(this);
        }

        var application = new ProjectionsApplication(results.ToImmutableDictionary());

        ActorSystem.RegisterExtension(application);

        return application;
    }
}