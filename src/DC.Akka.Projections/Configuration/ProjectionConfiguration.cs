using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public class ProjectionConfiguration<TId, TDocument>(
    string name,
    IProjectionStorage documentStorage,
    IProjectionPositionStorage positionStorage,
    IHandleEventInProjection<TId, TDocument> projectionsHandler,
    Func<long?, Source<EventWithPosition, NotUsed>> startSource,
    Func<object, Task<IActorRef>> createProjectionRef,
    Func<Task<IActorRef>> createProjectionCoordinator,
    RestartSettings? restartSettings,
    ProjectionStreamConfiguration projectionStreamConfiguration) 
    : ExtensionIdProvider<ProjectionConfiguration<TId, TDocument>>, IExtension
    where TId : notnull where TDocument : notnull
{
    public string Name { get; } = name;
    public IProjectionStorage DocumentStorage { get; } = documentStorage;
    public IProjectionPositionStorage PositionStorage { get; } = positionStorage;
    public IHandleEventInProjection<TId, TDocument> ProjectionsHandler { get; } = projectionsHandler;
    public Func<long?, Source<EventWithPosition, NotUsed>> StartSource { get; } = startSource;
    public Func<object, Task<IActorRef>> CreateProjectionRef { get; } = createProjectionRef;
    public Func<Task<IActorRef>> CreateProjectionCoordinator { get; } = createProjectionCoordinator;
    public RestartSettings? RestartSettings { get; } = restartSettings;
    public ProjectionStreamConfiguration ProjectionStreamConfiguration { get; } = projectionStreamConfiguration;
    
    public override ProjectionConfiguration<TId, TDocument> CreateExtension(ExtendedActorSystem system)
    {
        return this;
    }
}