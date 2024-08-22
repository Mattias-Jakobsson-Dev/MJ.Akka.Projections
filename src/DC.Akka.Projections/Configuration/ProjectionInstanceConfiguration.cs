using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public record ProjectionInstanceConfiguration(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy? EventBatchingStrategy,
    IProjectionStorage? ProjectionStorage,
    IProjectionPositionStorage? PositionStorage,
    IEventPositionBatchingStrategy? PositionBatchingStrategy) : ContinuousProjectionConfig(
    RestartSettings, EventBatchingStrategy, ProjectionStorage, PositionStorage, PositionBatchingStrategy)
{
    public static ProjectionInstanceConfiguration Empty { get; } = new(
        null,
        null,
        null,
        null,
        null);

    internal ProjectionInstanceConfiguration MergeWith(ProjectionSystemConfiguration parent)
    {
        return new ProjectionInstanceConfiguration(
            RestartSettings ?? parent.RestartSettings,
            EventBatchingStrategy ?? parent.EventBatchingStrategy,
            ProjectionStorage ?? parent.ProjectionStorage,
            PositionStorage ?? parent.PositionStorage,
            PositionBatchingStrategy ?? parent.PositionBatchingStrategy);
    }
}