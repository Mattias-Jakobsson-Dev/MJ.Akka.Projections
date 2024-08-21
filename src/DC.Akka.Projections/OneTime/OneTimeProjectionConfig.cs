using Akka.Streams;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.OneTime;

public record OneTimeProjectionConfig(
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration StreamConfiguration,
    long? StartPosition) : ProjectionConfig(RestartSettings, StreamConfiguration)
{
    public static OneTimeProjectionConfig Default { get; } = new(
        null,
        ProjectionStreamConfiguration.Default,
        null);
}