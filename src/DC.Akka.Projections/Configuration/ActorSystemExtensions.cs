using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public static class ActorSystemExtensions
{
    public static IProjectionsSetup Projections(this ActorSystem actorSystem)
    {
        return ProjectionsSetup.CreateDefault(actorSystem);
    }
}

