using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public record ProjectionConfiguration<TDocument>(
    string Name,
    bool AutoStart,
    IProjectionStorage Storage,
    IHandleEventInProjection<TDocument> ProjectionsHandler,
    Func<long?, Source<IProjectionSourceData, NotUsed>> StartSource,
    Func<object, Task<IActorRef>> CreateProjectionRef,
    RestartSettings RestartSettings,
    ProjectionStreamConfiguration ProjectionStreamConfiguration);