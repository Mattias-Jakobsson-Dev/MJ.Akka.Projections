using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public record ProjectionSystemConfiguration(
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration StreamConfiguration,
    IProjectionStorage ProjectionStorage,
    IProjectionPositionStorage PositionStorage,
    IConfigureProjectionCoordinator Coordinator,
    IKeepTrackOfProjectors ProjectorFactory,
    IImmutableDictionary<string, Func<ProjectionSystemConfiguration, ProjectionConfiguration>> Projections)
    : ProjectionConfig(RestartSettings, StreamConfiguration, ProjectionStorage, PositionStorage)
{
    public static ProjectionSystemConfiguration CreateDefaultConfiguration(ActorSystem actorSystem)
    {
        return new ProjectionSystemConfiguration(
            null,
            ProjectionStreamConfiguration.Default,
            new InMemoryProjectionStorage(),
            new InMemoryPositionStorage(),
            new InProcessSingletonProjectionCoordinator.Setup(actorSystem),
            new KeepTrackOfProjectorsInProc(actorSystem),
            ImmutableDictionary<string, Func<ProjectionSystemConfiguration, ProjectionConfiguration>>.Empty);
    }
}