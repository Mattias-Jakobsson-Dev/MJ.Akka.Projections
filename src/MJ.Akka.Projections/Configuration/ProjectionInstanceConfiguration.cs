using Akka.Streams;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public record ProjectionInstanceConfiguration(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy? EventBatchingStrategy,
    IEventPositionBatchingStrategy? PositionBatchingStrategy) : ContinuousProjectionConfig(
    RestartSettings, EventBatchingStrategy, PositionBatchingStrategy)
{
    public static ProjectionInstanceConfiguration Empty { get; } = new(
        null,
        null,
        null);

    internal ProjectionInstanceConfiguration MergeWith<TStorageSetup>(
        ProjectionSystemConfiguration<TStorageSetup> parent)
        where TStorageSetup : IStorageSetup
    {
        return new ProjectionInstanceConfiguration(
            RestartSettings ?? parent.RestartSettings,
            EventBatchingStrategy ?? parent.EventBatchingStrategy,
            PositionBatchingStrategy ?? parent.PositionBatchingStrategy);
    }
}