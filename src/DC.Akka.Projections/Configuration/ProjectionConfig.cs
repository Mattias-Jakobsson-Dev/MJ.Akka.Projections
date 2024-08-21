using Akka.Streams;

namespace DC.Akka.Projections.Configuration;

public abstract record ProjectionConfig(
    RestartSettings? RestartSettings,
    ProjectionStreamConfiguration? StreamConfiguration);