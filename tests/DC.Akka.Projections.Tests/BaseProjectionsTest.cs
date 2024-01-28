using System.Collections.Immutable;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using Xunit;

namespace DC.Akka.Projections.Tests;

public abstract class BaseProjectionsTest : TestKit, IAsyncLifetime
{
    private MutableTestProjection _projection = null!;
    protected TestInMemoryProjectionStorage Storage = null!;
    
    public async Task InitializeAsync()
    {
        _projection = new MutableTestProjection(WhenEvents());
        Storage = new TestInMemoryProjectionStorage();
        
        var documents = GivenDocuments();

        await Storage
            .StoreDocuments(documents
                .Select<MutableTestDocument, (ProjectedDocument, Action, Action<Exception?>)>(x => (
                    new ProjectedDocument(
                        x.Id,
                        x,
                        1),
                    () => { },
                    _ => { }))
                .ToImmutableList());
            
        var coordinator = await Configure(Sys.WithProjection(_projection))
            .Build();

        coordinator.Start();

        await coordinator.WaitForCompletion(TimeSpan.FromSeconds(5));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual IProjectionConfigurationSetup<MutableTestDocument> Configure(
        IProjectionConfigurationSetup<MutableTestDocument> config)
    {
        return config
            .WithStorage(Storage)
            .WithProjectionStreamConfiguration(new ProjectionStreamConfiguration(
                (10, TimeSpan.FromMilliseconds(100)),
                1,
                (10, TimeSpan.FromMilliseconds(100)),
                5));
    }
    
    protected virtual IImmutableList<MutableTestDocument> GivenDocuments()
    {
        return ImmutableList<MutableTestDocument>.Empty;
    }
    
    protected abstract IImmutableList<object> WhenEvents();

    protected Task<MutableTestDocument?> LoadDocument(string id)
    {
        return Storage.LoadDocument<MutableTestDocument>(id);
    }

    protected Task<IImmutableList<object>> LoadAllDocuments()
    {
        return Storage.LoadAll();
    }
    
    protected Task<long?> LoadPosition()
    {
        return Storage.LoadLatestPosition(_projection.Name);
    }
}