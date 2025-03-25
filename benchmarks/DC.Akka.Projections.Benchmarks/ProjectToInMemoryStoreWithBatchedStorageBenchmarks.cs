using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Benchmarks;

[PublicAPI]
public class ProjectToInMemoryStoreWithBatchedStorageBenchmarks : BaseProjectionBenchmarks
{
    protected override IHaveConfiguration<ProjectionInstanceConfiguration> Configure(
        IHaveConfiguration<ProjectionInstanceConfiguration> config)
    {
        return config
            .WithInMemoryStorage()
            .Batched();
    }
}