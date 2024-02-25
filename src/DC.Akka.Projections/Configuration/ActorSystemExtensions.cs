using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public static class ActorSystemExtensions
{
    public static ProjectionsApplication Projections(this ActorSystem actorSystem)
    {
        return actorSystem.WithExtension<ProjectionsApplication, ProjectionsApplication.Provider>();
    }
}

