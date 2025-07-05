using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using MJ.Akka.Projections.InProc;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public record ProjectionSystemConfiguration(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy EventBatchingStrategy,
    IEventPositionBatchingStrategy PositionBatchingStrategy,
    IConfigureProjectionCoordinator Coordinator,
    IKeepTrackOfProjectors ProjectorFactory,
    IImmutableDictionary<string, Func<ProjectionSystemConfiguration, ProjectionConfiguration>> Projections)
    : ContinuousProjectionConfig(
        RestartSettings,
        EventBatchingStrategy,
        PositionBatchingStrategy)
{
    public static ProjectionSystemConfiguration CreateDefaultConfiguration(ActorSystem actorSystem)
    {
        return new ProjectionSystemConfiguration(
            null,
            BatchWithinEventBatchingStrategy.Default,
            BatchWithinEventPositionBatchingStrategy.Default,
            new InProcessSingletonProjectionCoordinator.Setup(actorSystem),
            new KeepTrackOfProjectorsInProc(actorSystem, MaxNumberOfProjectorsPassivation.Default),
            ImmutableDictionary<string, Func<ProjectionSystemConfiguration, ProjectionConfiguration>>.Empty);
    }
}