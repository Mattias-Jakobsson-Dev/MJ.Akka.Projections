using Akka.Actor;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public interface IHaveActorSystem
{
    ActorSystem StartNewActorSystem();
}