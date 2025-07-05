using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithNormalInMemoryStorageWithGroupedWithinEventStrategyTests
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseContinuousTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
        {
            return config
                .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(
                    100,
                    TimeSpan.FromMilliseconds(100)));
        }
    }

    public class With_int_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseContinuousTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
        {
            return config
                .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(
                    100,
                    TimeSpan.FromMilliseconds(100)));
        }
    }
}