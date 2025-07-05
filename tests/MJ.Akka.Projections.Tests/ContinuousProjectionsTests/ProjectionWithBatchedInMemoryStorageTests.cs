using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithBatchedInMemoryStorageTests
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseContinuousTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
        {
            return config.WithBatchedStorage();
        }
    }

    public class With_int_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseContinuousTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
        {
            return config.WithBatchedStorage();
        }
    }
}