using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionConfiguration
{
    IProjection Projection { get; }
    IProjectionStorage DocumentStorage { get; }
    IProjectionPositionStorage PositionStorage { get; }
    Func<long?, Source<EventWithPosition, NotUsed>> StartSource { get; }
    IKeepTrackOfProjectors ProjectorFactory { get; }
    IStartProjectionCoordinator ProjectionCoordinatorStarter { get; }
    RestartSettings? RestartSettings { get; }
    ProjectionStreamConfiguration ProjectionStreamConfiguration { get; }
}