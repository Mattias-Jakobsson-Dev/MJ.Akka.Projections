using Akka.Actor;

namespace DC.Akka.Projections.Tests;

public interface IHaveActorSystem
{
    ActorSystem StartNewActorSystem();
}