using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Benchmarks;

[PublicAPI]
public class ProjectToInMemoryStoreWithNormalStorageBenchmarks 
    : BaseProjectionBenchmarks<string, InMemoryProjectionContext<string, InMemoryTestProjection.TestDocument>, SetupInMemoryStorage>
{
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