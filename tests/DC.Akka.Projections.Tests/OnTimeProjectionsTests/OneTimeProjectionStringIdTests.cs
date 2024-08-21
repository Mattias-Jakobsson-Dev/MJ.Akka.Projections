using Xunit;

namespace DC.Akka.Projections.Tests.OnTimeProjectionsTests;

public class OneTimeProjectionStringIdTests(NormalTestKitActorSystem actorSystemSupplier) 
    : TestProjectionBaseOneTimeTests<string>(actorSystemSupplier), IClassFixture<NormalTestKitActorSystem>;