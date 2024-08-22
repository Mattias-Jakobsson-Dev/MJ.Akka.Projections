using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public abstract record ContinuousProjectionConfig(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy? EventBatchingStrategy,
    IProjectionStorage? ProjectionStorage,
    IProjectionPositionStorage? PositionStorage,
    IEventPositionBatchingStrategy? PositionBatchingStrategy) 
    : ProjectionConfig(RestartSettings, EventBatchingStrategy);