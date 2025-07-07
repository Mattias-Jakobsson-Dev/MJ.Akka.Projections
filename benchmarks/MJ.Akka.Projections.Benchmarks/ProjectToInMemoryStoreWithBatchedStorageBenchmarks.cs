using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Benchmarks;

[PublicAPI]
public class ProjectToInMemoryStoreWithBatchedStorageBenchmarks 
    : BaseProjectionBenchmarks<string, InMemoryProjectionContext<string, InMemoryTestProjection.TestDocument>, SetupInMemoryStorage>
{
    protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> ConfigureSystem(
        IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> config)
    {
        return config.WithBatchedStorage();
    }

    protected override SetupInMemoryStorage GetStorageSetup()
    {
        return new SetupInMemoryStorage();
    }

    protected override IProjection<string, InMemoryProjectionContext<string, InMemoryTestProjection.TestDocument>, SetupInMemoryStorage> 
        CreateProjection(int numberOfEvents, int numberOfDocuments)
    {
        return new InMemoryTestProjection(numberOfEvents, numberOfDocuments);
    }
}