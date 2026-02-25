using BenchmarkDotNet.Attributes;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.RavenDb;
using MJ.Akka.Projections.Tests;
using Raven.Client.Documents.BulkInsert;

namespace MJ.Akka.Projections.Benchmarks;

public class ProjectToRavenDbStoreWithBatchedStorageBenchmarks 
    : BaseProjectionBenchmarks<SimpleIdContext<string>, RavenDbProjectionContext<RavenDbTestProjection.TestDocument, SimpleIdContext<string>>, SetupRavenDbStorage>
{
    private RavenDbDockerContainerFixture _containerFixture = null!;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _containerFixture = new RavenDbDockerContainerFixture();

        await _containerFixture.InitializeAsync();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _containerFixture.DisposeAsync();
    }
    
    protected override IHaveConfiguration<ProjectionSystemConfiguration<SetupRavenDbStorage>> ConfigureSystem(
        IHaveConfiguration<ProjectionSystemConfiguration<SetupRavenDbStorage>> config)
    {
        return config.WithBatchedStorage();
    }
    
    protected override SetupRavenDbStorage GetStorageSetup()
    {
        var databaseName = Guid.NewGuid().ToString();

        var documentStore = _containerFixture.CreateDocumentStore(databaseName);

        documentStore.EnsureDatabaseExists();

        return new SetupRavenDbStorage(documentStore, new BulkInsertOptions());
    }

    protected override IProjection<SimpleIdContext<string>, RavenDbProjectionContext<RavenDbTestProjection.TestDocument, SimpleIdContext<string>>, SetupRavenDbStorage> 
        CreateProjection(int numberOfEvents, int numberOfDocuments)
    {
        return new RavenDbTestProjection(numberOfEvents, numberOfDocuments);
    }
}