using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public static class ActorSystemExtensions
{
    public static IProjectionConfigurationSetup<TDocument> WithProjection<TDocument>(
        this ActorSystem actorSystem,
        IProjection<TDocument> projection)
    {
        return new ProjectionConfigurationSetup<TDocument>(projection, actorSystem);
    }
}