using System.Collections.Immutable;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.Messages;

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

        return await loadStorage.Load(typedId, cancellationToken);
    }

    public override async Task Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await storage.Store(request, cancellationToken);

        if (!response.Completed)
            throw new StoreProjectionException();
    }

    public override IImmutableList<object> TransformEvent(object evnt)
    {
        return eventsHandler.Transform(evnt);
    }

    public override DocumentId GetDocumentIdFrom(object evnt)
    {
        return eventsHandler.GetDocumentIdFrom(evnt);
    }

    public override async Task<(bool handled, IImmutableList<IProjectionResult> results)> HandleEvent(
        object context,
        object evnt,
        long position,
        CancellationToken cancellationToken)
    {
        var response = await eventsHandler.Handle(
            (TContext)context,
            evnt, 
            position,
            cancellationToken);

        return response;
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
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default);
    
    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return _projection.StartSource(fromPosition);
    }
    
    public abstract IImmutableList<object> TransformEvent(object evnt);
    
    public abstract DocumentId GetDocumentIdFrom(object evnt);
    
    public abstract Task<(bool handled, IImmutableList<IProjectionResult> results)> HandleEvent(
        object context,
        object evnt,
        long position,
        CancellationToken cancellationToken);
}
