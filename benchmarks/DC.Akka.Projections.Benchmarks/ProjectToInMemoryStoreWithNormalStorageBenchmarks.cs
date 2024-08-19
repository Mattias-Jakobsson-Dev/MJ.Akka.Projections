using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Benchmarks;

[PublicAPI]
public class ProjectToInMemoryStoreWithNormalStorageBenchmarks : BaseProjectionBenchmarks
{
    protected override IHaveConfiguration<ProjectionInstanceConfiguration> Configure(
        IHaveConfiguration<ProjectionInstanceConfiguration> config)
    {
        return config;
    }
}