using Akka.Streams;

namespace MJ.Akka.Projections.Configuration;

public abstract record ContinuousProjectionConfig(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy? EventBatchingStrategy,
    IEventPositionBatchingStrategy? PositionBatchingStrategy) 
    : ProjectionConfig(RestartSettings, EventBatchingStrategy);