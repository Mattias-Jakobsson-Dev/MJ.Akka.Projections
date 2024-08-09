using System.Collections.Immutable;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace DC.Akka.Projections.Tests;

public abstract class BaseProjectionsTest<TId>(ITestOutputHelper output) : TestKit(output: output), IAsyncLifetime 
    where TId : notnull
{
    private TestProjection<TId> _projection = null!;
    protected TestInMemoryProjectionStorage Storage = null!;
    [PublicAPI]
    protected IProjectionPositionStorage PositionStorage = null!;
    
    public async Task InitializeAsync()
    {
        _projection = new TestProjection<TId>(WhenEvents());
        Storage = new TestInMemoryProjectionStorage();
        PositionStorage = new InMemoryPositionStorage();
        
        var documents = GivenDocuments();

        await Storage
            .Store(
                documents
                    .Select(x => new DocumentToStore(x.Id, x))
                    .ToImmutableList(),
                ImmutableList<DocumentToDelete>.Empty);
        
        var projectionsApplication = await Sys
            .Projections()
            .WithProjection(_projection, Configure)
            .Start();
        
        await projectionsApplication.GetProjection(_projection.Name)!.WaitForCompletion(TimeSpan.FromSeconds(5));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual IProjectionConfigurationSetup<TId, TestDocument<TId>> Configure(
        IProjectionConfigurationSetup<TId, TestDocument<TId>> config)
    {
        return config
            .WithProjectionStorage(Storage)
            .WithPositionStorage(PositionStorage)
            .WithProjectionStreamConfiguration(new ProjectionStreamConfiguration(
                10,
                1,
                (10, TimeSpan.FromMilliseconds(100)),
                5,
                TimeSpan.FromSeconds(30)));
    }
    
    [PublicAPI]
    protected virtual IImmutableList<TestDocument<TId>> GivenDocuments()
    {
        return ImmutableList<TestDocument<TId>>.Empty;
    }
    
    protected abstract IImmutableList<object> WhenEvents();

    protected async Task<TestDocument<TId>?> LoadDocument(TId id)
    {
        var (document, _) = await Storage.LoadDocument<TestDocument<TId>>(id);

        return document;
    }

    protected Task<IImmutableList<object>> LoadAllDocuments()
    {
        return Storage.LoadAll();
    }
    
    protected Task<long?> LoadPosition()
    {
        return PositionStorage.LoadLatestPosition(_projection.Name);
    }
}