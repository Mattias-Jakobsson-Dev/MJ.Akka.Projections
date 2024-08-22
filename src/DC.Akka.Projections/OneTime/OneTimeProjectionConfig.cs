using Akka.Streams;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.OneTime;

public record OneTimeProjectionConfig(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy EventBatchingStrategy,
    long? StartPosition) : ProjectionConfig(RestartSettings, EventBatchingStrategy)
{
    public static OneTimeProjectionConfig Default { get; } = new(
        null,
        BatchEventBatchingStrategy.Default,
        null);
}