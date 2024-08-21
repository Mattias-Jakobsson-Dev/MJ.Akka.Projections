using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public abstract record ContinuousProjectionConfig(
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration? StreamConfiguration,
    IProjectionStorage? ProjectionStorage,
    IProjectionPositionStorage? PositionStorage) 
    : ProjectionConfig(RestartSettings, StreamConfiguration);