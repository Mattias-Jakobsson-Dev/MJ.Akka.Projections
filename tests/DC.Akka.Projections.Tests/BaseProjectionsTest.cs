using System.Collections.Immutable;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using Xunit;

namespace DC.Akka.Projections.Tests;

public abstract class BaseProjectionsTest : TestKit, IAsyncLifetime
{
    private TestProjection _projection = null!;
    protected TestInMemoryProjectionStorage<TestDocument> Storage = null!;
    protected IProjectionPositionStorage PositionStorage = null!;
    
    public async Task InitializeAsync()
    {
        _projection = new TestProjection(WhenEvents());
        Storage = new TestInMemoryProjectionStorage<TestDocument>();
        PositionStorage = new InMemoryProjectionPositionStorage();

        var projectionsApplication = Sys.Projections();
        
        var documents = GivenDocuments();

        var transaction = await Storage.StartTransaction(
            documents
                .Select(x => (x.Id, x, ActorRefs.NoSender)).ToImmutableList(),
            ImmutableList<(string id, IActorRef ackTo)>.Empty);

        await transaction.Commit();

        var coordinator = await projectionsApplication.WithProjection(_projection, Configure);

        coordinator.Start();

        await coordinator.WaitForCompletion(TimeSpan.FromSeconds(5));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual IProjectionConfigurationSetup<string, TestDocument> Configure(
        IProjectionConfigurationSetup<string, TestDocument> config)
    {
        return config
            .WithProjectionStorage(Storage)
            .WithPositionStorage(PositionStorage)
            .WithProjectionStreamConfiguration(new ProjectionStreamConfiguration(
                (10, TimeSpan.FromMilliseconds(100)),
                1,
                (10, TimeSpan.FromMilliseconds(100)),
                5));
    }
    
    protected virtual IImmutableList<TestDocument> GivenDocuments()
    {
        return ImmutableList<TestDocument>.Empty;
    }
    
    protected abstract IImmutableList<object> WhenEvents();

    protected async Task<TestDocument?> LoadDocument(string id)
    {
        var (document, _) = await Storage.LoadDocument(id);

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