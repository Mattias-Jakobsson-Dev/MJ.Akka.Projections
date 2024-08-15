using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public class ProjectionConfiguration<TId, TDocument>(
    IProjection<TId, TDocument> projection,
    IProjectionStorage documentStorage,
    IProjectionPositionStorage positionStorage,
    IHandleEventInProjection<TId, TDocument> projectionsHandler,
    Func<long?, Source<EventWithPosition, NotUsed>> startSource,
    IKeepTrackOfProjectors projectorFactory,
    IStartProjectionCoordinator projectionCoordinatorStarter,
    RestartSettings? restartSettings,
    ProjectionStreamConfiguration projectionStreamConfiguration) 
    : ExtensionIdProvider<ProjectionConfiguration<TId, TDocument>>, IExtension, IProjectionConfiguration
    where TId : notnull where TDocument : notnull
{
    public IProjection<TId, TDocument> Projection { get; } = projection;
    IProjection IProjectionConfiguration.Projection { get; } = projection;
    public IProjectionStorage DocumentStorage { get; } = documentStorage;
    public IProjectionPositionStorage PositionStorage { get; } = positionStorage;
    public IHandleEventInProjection<TId, TDocument> ProjectionsHandler { get; } = projectionsHandler;
    public Func<long?, Source<EventWithPosition, NotUsed>> StartSource { get; } = startSource;
    public IKeepTrackOfProjectors ProjectorFactory { get; } = projectorFactory;
    public IStartProjectionCoordinator ProjectionCoordinatorStarter { get; } = projectionCoordinatorStarter;
    public RestartSettings? RestartSettings { get; } = restartSettings;
    public ProjectionStreamConfiguration ProjectionStreamConfiguration { get; } = projectionStreamConfiguration;
    
    public override ProjectionConfiguration<TId, TDocument> CreateExtension(ExtendedActorSystem system)
    {
        return this;
    }
}