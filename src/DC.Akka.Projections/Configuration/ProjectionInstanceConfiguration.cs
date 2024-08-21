using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public record ProjectionInstanceConfiguration(
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration? StreamConfiguration,
    IProjectionStorage? ProjectionStorage,
    IProjectionPositionStorage? PositionStorage) : ContinuousProjectionConfig(
    RestartSettings, StreamConfiguration, ProjectionStorage, PositionStorage)
{
    public static ProjectionInstanceConfiguration Empty { get; } = new(
        null,
        null,
        null,
        null);

    internal ProjectionInstanceConfiguration MergeWith(ProjectionSystemConfiguration parent)
    {
        return new ProjectionInstanceConfiguration(
            RestartSettings ?? parent.RestartSettings,
            StreamConfiguration ?? parent.StreamConfiguration,
            ProjectionStorage ?? parent.ProjectionStorage,
            PositionStorage ?? parent.PositionStorage);
    }
}