using Akka.Actor;

namespace MJ.Akka.Projections.Tests;

public interface IHaveActorSystem
{
    ActorSystem StartNewActorSystem();
}