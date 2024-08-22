using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using Xunit;

namespace DC.Akka.Projections.Tests.ContinuousProjectionsTests;

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
                    TimeSpan.FromMilliseconds(100),
                    100));
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
                    TimeSpan.FromMilliseconds(100),
                    100));
        }
    }
}