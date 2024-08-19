using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public abstract record ProjectionConfig(
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration? StreamConfiguration,
    IProjectionStorage? ProjectionStorage,
    IProjectionPositionStorage? PositionStorage);