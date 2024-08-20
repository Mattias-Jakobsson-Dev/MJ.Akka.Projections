using Akka.Actor;
using DC.Akka.Projections.Storage;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithBatchedInMemoryStorageTests
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseFlowTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IProjectionStorage GetProjectionStorage(ActorSystem system)
        {
            return base.GetProjectionStorage(system).Batched(system, 100, 5);
        }
    }

    public class With_int_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseFlowTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IProjectionStorage GetProjectionStorage(ActorSystem system)
        {
            return base.GetProjectionStorage(system).Batched(system, 100, 5);
        }
    }
}