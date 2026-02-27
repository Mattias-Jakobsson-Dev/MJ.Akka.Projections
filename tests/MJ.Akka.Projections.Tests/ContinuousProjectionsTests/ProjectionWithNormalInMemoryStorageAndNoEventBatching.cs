using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithNormalInMemoryStorageAndNoEventBatching
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup)
        : TestProjectionBaseContinuousTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
        {
            return base.Configure(config)
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(100))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy());
        }
    }

    public class With_int_id(NormalTestKitActorSystem actorSystemSetup)
        : TestProjectionBaseContinuousTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
        {
            return base.Configure(config)
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(100))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy());
        }
    }
}