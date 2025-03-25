using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Benchmarks;

[PublicAPI]
public class ProjectToInMemoryStoreWithNormalStorageBenchmarks : BaseProjectionBenchmarks
{
    protected override IHaveConfiguration<ProjectionInstanceConfiguration> Configure(
        IHaveConfiguration<ProjectionInstanceConfiguration> config)
    {
        return config;
    }
}