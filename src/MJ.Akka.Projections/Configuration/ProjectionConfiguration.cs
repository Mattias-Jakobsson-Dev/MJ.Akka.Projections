using System.Collections.Immutable;
using Akka.Streams;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public class ProjectionConfiguration<TIdContext, TContext, TStorageSetup>(
    IProjection<TIdContext, TContext, TStorageSetup> projection,
    IProjectionStorage storage,
    ILoadProjectionContext<TIdContext, TContext> loadStorage,
    IProjectionPositionStorage positionStorage,
    IProjectionStashStorage stashStorage,
    IKeepTrackOfProjectors projectorFactory,
    RestartSettings? restartSettings,
    IEventBatchingStrategy projectionEventBatchingStrategy,
    IEventPositionBatchingStrategy positionBatchingStrategy,
    IHandleEventInProjection<TIdContext, TContext> eventsHandler) 
    : ProjectionConfiguration(
        projection,
        positionStorage,
        stashStorage,
        projectorFactory,
        restartSettings,
        projectionEventBatchingStrategy,
        positionBatchingStrategy) 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    public override async Task<IProjectionContext> Load(object id, CancellationToken cancellationToken = default)
    {
        if (id is not TIdContext typedId)
        {
            throw new InvalidProjectionTypeException(
                typeof(TIdContext), 
                id.GetType(), 
                projection.GetType(), 
                "id");
        }

        return await loadStorage.Load(typedId, projection.GetDefaultContext, cancellationToken);
    }

    public override Task Store(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        CancellationToken cancellationToken = default)
    {
        return storage.Store(contexts, cancellationToken);
    }
    
    public override Task<IImmutableList<object>> TransformEvent(object evnt)
    {
        return eventsHandler.Transform(evnt);
    }

    public override Task<object> PrepareEvent(object evnt)
    {
        return eventsHandler.PrepareEvent(evnt);
    }

    public override IProjectionIdContext? GetIdContextFor(object evnt)
    {
        return eventsHandler.GetIdContextFor(evnt);
    }

    public override Task<bool> HandleEvent(
        IProjectionContext context,
        object evnt,
        long position,
        ProjectionStashContext stashContext,
        CancellationToken cancellationToken)
    {
        return eventsHandler.Handle(
            (TContext)context,
            evnt, 
            position,
            stashContext,
            cancellationToken);
    }
}

public abstract class ProjectionConfiguration
{
    private readonly IProjection _projection;
    
    internal ProjectionConfiguration(
        IProjection projection,
        IProjectionPositionStorage positionStorage,
        IProjectionStashStorage stashStorage,
        IKeepTrackOfProjectors projectorFactory,
        RestartSettings? restartSettings,
        IEventBatchingStrategy projectionEventBatchingStrategy,
        IEventPositionBatchingStrategy positionBatchingStrategy)
    {
        _projection = projection;
        PositionStorage = positionStorage;
        StashStorage = stashStorage;
        ProjectorFactory = projectorFactory;
        RestartSettings = restartSettings;
        ProjectionEventBatchingStrategy = projectionEventBatchingStrategy;
        PositionBatchingStrategy = positionBatchingStrategy;
    }
    
    public string Name => _projection.Name;
    public IProjectionPositionStorage PositionStorage { get; }
    public IProjectionStashStorage StashStorage { get; }
    public IKeepTrackOfProjectors ProjectorFactory { get; }
    public RestartSettings? RestartSettings { get; }
    public IEventBatchingStrategy ProjectionEventBatchingStrategy { get; }
    public IEventPositionBatchingStrategy PositionBatchingStrategy { get; }

    public IProjection GetProjection()
    {
        return _projection;
    }
    
    public abstract Task<IProjectionContext> Load(
        object id,
        CancellationToken cancellationToken = default);
    
    public abstract Task Store(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        CancellationToken cancellationToken = default);
    
    public Task<IProjectionEventSource> GetSource()
    {
        return _projection.GetSource();
    }
    
    public abstract Task<IImmutableList<object>> TransformEvent(object evnt);

    public abstract Task<object> PrepareEvent(object evnt);

    public abstract IProjectionIdContext? GetIdContextFor(object evnt);
    
    public abstract Task<bool> HandleEvent(
        IProjectionContext context,
        object evnt,
        long position,
        ProjectionStashContext stashContext,
        CancellationToken cancellationToken);
}
