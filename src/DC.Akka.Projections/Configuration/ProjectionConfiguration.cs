using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public record ProjectionConfiguration<TId, TDocument>(
    string Name,
    bool AutoStart,
    IProjectionStorage DocumentStorage,
    IProjectionPositionStorage PositionStorage,
    IHandleEventInProjection<TId, TDocument> ProjectionsHandler,
    Func<long?, Source<EventWithPosition, NotUsed>> StartSource,
    Func<TId, Task<IActorRef>> CreateProjectionRef,
    Func<Task<IActorRef>> CreateProjectionCoordinator,
    RestartSettings RestartSettings,
    ProjectionStreamConfiguration ProjectionStreamConfiguration) where TId : notnull where TDocument : notnull;