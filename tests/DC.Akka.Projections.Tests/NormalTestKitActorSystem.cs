using Akka.Actor;
using Akka.TestKit.Xunit2;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests;

[PublicAPI]
public class NormalTestKitActorSystem : TestKit, IHaveActorSystem
{
    public ActorSystem StartNewActorSystem()
    {
        return ActorSystem.Create(Sys.Name, DefaultConfig);
    }
}