using Xunit;

namespace DC.Akka.Projections.Tests.OnTimeProjectionsTests;

public class OneTimeProjectionIntIdTests(NormalTestKitActorSystem actorSystemSupplier) 
    : TestProjectionBaseOneTimeTests<int>(actorSystemSupplier), IClassFixture<NormalTestKitActorSystem>;