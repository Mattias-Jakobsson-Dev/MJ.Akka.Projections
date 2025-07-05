using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using MJ.Akka.Projections.InProc;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public record ProjectionSystemConfiguration<TStorageSetup>(
    TStorageSetup StorageSetup,
    IImmutableList<IModifyStorage> StorageModifiers,
    RestartSettings? RestartSettings,
    IEventBatchingStrategy EventBatchingStrategy,
    IEventPositionBatchingStrategy PositionBatchingStrategy,
    IConfigureProjectionCoordinator Coordinator,
    IKeepTrackOfProjectors ProjectorFactory,
    IImmutableDictionary<string, Func<ProjectionSystemConfiguration<TStorageSetup>, ProjectionConfiguration>> Projections)
    : ContinuousProjectionConfig(
        RestartSettings,
        EventBatchingStrategy,
        PositionBatchingStrategy) where TStorageSetup : IStorageSetup
{
    public static ProjectionSystemConfiguration<TStorageSetup> CreateDefaultConfiguration(
        ActorSystem actorSystem,
        TStorageSetup storageSetup)
    {
        return new ProjectionSystemConfiguration<TStorageSetup>(
            storageSetup,
            ImmutableList<IModifyStorage>.Empty, 
            null,
            BatchWithinEventBatchingStrategy.Default,
            BatchWithinEventPositionBatchingStrategy.Default,
            new InProcessSingletonProjectionCoordinator.Setup(actorSystem),
            new KeepTrackOfProjectorsInProc(actorSystem, MaxNumberOfProjectorsPassivation.Default),
            ImmutableDictionary<string, Func<ProjectionSystemConfiguration<TStorageSetup>, ProjectionConfiguration>>.Empty);
    }
}