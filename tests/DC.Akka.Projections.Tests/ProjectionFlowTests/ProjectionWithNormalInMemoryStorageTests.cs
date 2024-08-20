using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithNormalInMemoryStorageTests
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseFlowTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>;
    
    public class With_int_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseFlowTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>;
}