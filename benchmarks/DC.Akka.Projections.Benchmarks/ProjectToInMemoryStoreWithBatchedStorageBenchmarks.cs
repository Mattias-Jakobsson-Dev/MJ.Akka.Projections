using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Benchmarks;

[PublicAPI]
public class ProjectToInMemoryStoreWithBatchedStorageBenchmarks : BaseProjectionBenchmarks
{
    protected override IProjectionConfigurationSetup<string, TestProjection.TestDocument> Configure(
        IProjectionConfigurationSetup<string, TestProjection.TestDocument> config)
    {
        return config
            .WithInMemoryStorage()
            .Batched();
    }
}