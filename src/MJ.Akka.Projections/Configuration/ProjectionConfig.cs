using Akka.Streams;

namespace MJ.Akka.Projections.Configuration;

public abstract record ProjectionConfig(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy? EventBatchingStrategy);