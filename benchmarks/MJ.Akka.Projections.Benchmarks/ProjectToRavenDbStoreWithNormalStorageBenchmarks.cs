using BenchmarkDotNet.Attributes;
using MJ.Akka.Projections.Storage.RavenDb;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Tests;

namespace MJ.Akka.Projections.Benchmarks;

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