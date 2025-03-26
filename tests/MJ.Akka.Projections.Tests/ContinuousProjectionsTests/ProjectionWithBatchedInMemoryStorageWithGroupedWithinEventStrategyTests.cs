using MJ.Akka.Projections.Configuration;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithBatchedInMemoryStorageWithGroupedWithinEventStrategyTests
{
    public class With_string_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseContinuousTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> config)
        {
            return config
                .WithInMemoryStorage()
                .Batched()
                .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(
                    100,
                    TimeSpan.FromMilliseconds(100)));
        }
    }

    public class With_int_id(NormalTestKitActorSystem actorSystemSetup) 
        : TestProjectionBaseContinuousTests<int>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> config)
        {
            return config
                .WithInMemoryStorage()
                .Batched()
                .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(
                    100,
                    TimeSpan.FromMilliseconds(100)));
        }
    }
}