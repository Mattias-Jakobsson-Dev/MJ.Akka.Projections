using BenchmarkDotNet.Attributes;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage.RavenDb;
using DC.Akka.Projections.Tests;

namespace DC.Akka.Projections.Benchmarks;

public class ProjectToRavenDbStoreWithNormalStorageBenchmarks : BaseProjectionBenchmarks
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
    
    protected override IHaveConfiguration<ProjectionInstanceConfiguration> Configure(
        IHaveConfiguration<ProjectionInstanceConfiguration> config)
    {
        var databaseName = Guid.NewGuid().ToString();

        var documentStore = _containerFixture.CreateDocumentStore(databaseName);

        documentStore.EnsureDatabaseExists();

        return config
            .WithRavenDbPositionStorage(documentStore)
            .WithRavenDbDocumentStorage(documentStore);
    }
}