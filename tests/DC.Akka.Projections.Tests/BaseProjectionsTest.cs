using System.Collections.Immutable;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests;

public abstract class BaseProjectionsTest<TId> : TestKit, IAsyncLifetime where TId : notnull
{
    private TestProjection<TId> _projection = null!;
    protected TestInMemoryPositionStorage<TId, TestDocument<TId>> Storage = null!;
    [PublicAPI]
    protected IProjectionPositionStorage PositionStorage = null!;
    
    public async Task InitializeAsync()
    {
        _projection = new TestProjection<TId>(WhenEvents());
        Storage = new TestInMemoryPositionStorage<TId, TestDocument<TId>>();
        PositionStorage = new InMemoryProjectionPositionStorage();

        var projectionsApplication = Sys.Projections();
        
        var documents = GivenDocuments();

        var transaction = await Storage.StartTransaction(
            documents
                .Select(x => (x.Id, x, ActorRefs.NoSender))
                .ToImmutableList(),
            ImmutableList<(TId id, IActorRef ackTo)>.Empty);

        await transaction.Commit();

        var coordinator = await projectionsApplication.WithProjection(_projection, Configure);
        
        await coordinator.RunToCompletion(TimeSpan.FromSeconds(5));
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