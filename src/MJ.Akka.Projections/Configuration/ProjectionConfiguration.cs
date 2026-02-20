using System.Collections.Immutable;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public class ProjectionConfiguration<TId, TContext, TStorageSetup>(
    IProjection<TId, TContext, TStorageSetup> projection,
    IProjectionStorage storage,
    ILoadProjectionContext<TId, TContext> loadStorage,
    IProjectionPositionStorage positionStorage,
    IKeepTrackOfProjectors projectorFactory,
    RestartSettings? restartSettings,
    IEventBatchingStrategy projectionEventBatchingStrategy,
    IEventPositionBatchingStrategy positionBatchingStrategy,
    IHandleEventInProjection<TId, TContext> eventsHandler) 
    : ProjectionConfiguration(
        projection,
        positionStorage,
        projectorFactory,
        restartSettings,
        projectionEventBatchingStrategy,
        positionBatchingStrategy) 
    where TId : notnull where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    public override async Task<IProjectionContext> Load(object id, CancellationToken cancellationToken = default)
    {
        if (id is not TId typedId)
        {
            throw new InvalidProjectionTypeException(
                typeof(TId), 
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
    
    public override IImmutableList<object> TransformEvent(object evnt)
    {
        return eventsHandler.Transform(evnt);
    }

    public override Task<DocumentId> GetDocumentIdFrom(object evnt)
    {
        return eventsHandler.GetDocumentIdFrom(evnt);
    }

    public override Task<bool> HandleEvent(
        IProjectionContext context,
        object evnt,
        long position,
        CancellationToken cancellationToken)
    {
        return eventsHandler.Handle(
            (TContext)context,
            evnt, 
            position,
            cancellationToken);
    }
}

public abstract class ProjectionConfiguration
{
    private readonly IProjection _projection;
    
    internal ProjectionConfiguration(
        IProjection projection,
        IProjectionPositionStorage positionStorage,
        IKeepTrackOfProjectors projectorFactory,
        RestartSettings? restartSettings,
        IEventBatchingStrategy projectionEventBatchingStrategy,
        IEventPositionBatchingStrategy positionBatchingStrategy)
    {
        _projection = projection;
        PositionStorage = positionStorage;
        ProjectorFactory = projectorFactory;
        RestartSettings = restartSettings;
        ProjectionEventBatchingStrategy = projectionEventBatchingStrategy;
        PositionBatchingStrategy = positionBatchingStrategy;
    }
    
    public string Name => _projection.Name;
    public IProjectionPositionStorage PositionStorage { get; }
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
    
    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return _projection.StartSource(fromPosition ?? _projection.GetInitialPosition());
    }
    
    public abstract IImmutableList<object> TransformEvent(object evnt);
    
    public abstract Task<DocumentId> GetDocumentIdFrom(object evnt);
    
    public abstract Task<bool> HandleEvent(
        IProjectionContext context,
        object evnt,
        long position,
        CancellationToken cancellationToken);
}
