using Akka.Streams;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public abstract record ContinuousProjectionConfig(
    RestartSettings? RestartSettings,
    IEventBatchingStrategy? EventBatchingStrategy,
    IProjectionStorage? ProjectionStorage,
    IProjectionPositionStorage? PositionStorage,
    IEventPositionBatchingStrategy? PositionBatchingStrategy) 
    : ProjectionConfig(RestartSettings, EventBatchingStrategy);