using DC.Akka.Projections.Configuration;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithBatchedInMemoryStorageTests
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseFlowTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> config)
        {
            return config
                .WithInMemoryStorage()
                .Batched();
        }
    }

    public class With_int_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseFlowTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> config)
        {
            return config
                .WithInMemoryStorage()
                .Batched();
        }
    }
}