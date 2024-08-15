using System.Collections.Immutable;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionCoordinator;

public abstract class ProjectionCoordinatorTestsBase : TestKit, IAsyncLifetime
{
    private InMemoryPositionStorage _positionStorage = null!;
    protected TestInMemoryProjectionStorage Storage = null!;

    public async Task InitializeAsync()
    {
        _positionStorage = new InMemoryPositionStorage();
        Storage = new TestInMemoryProjectionStorage();

        var initialData = Given(GivenConfiguration.Empty);

        foreach (var position in initialData.InitialPositions)
            await _positionStorage.StoreLatestPosition(position.projectionName, position.position);

        await Storage
            .Store(
                initialData
                    .InitialDocuments
                    .Select(x => new DocumentToStore(x.id, x.document))
                    .ToImmutableList(),
                ImmutableList<DocumentToDelete>.Empty);

        var setup = Configure(Sys
            .Projections()
            .WithProjectionStorage(Storage)
            .WithPositionStorage(_positionStorage));

        var projections = (await setup.Start()).GetProjections();

        foreach (var projection in projections)
            await projection.Value.WaitForCompletion(TimeSpan.FromSeconds(5));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    public Task<TDocument?> LoadDocument<TDocument>(object id)
    {
        return Storage.LoadDocument<TDocument>(id);
    }

    public Task<IImmutableList<object>> LoadAllDocuments()
    {
        return Storage.LoadAll();
    }

    public async Task<long?> LoadPosition(string projectionName)
    {
        var position = await _positionStorage.LoadLatestPosition(projectionName);

        return position;
    }

    protected abstract IProjectionsSetup Configure(IProjectionsSetup setup);

    [PublicAPI]
    protected virtual GivenConfiguration Given(GivenConfiguration config)
    {
        return config;
    }
    
    [PublicAPI]
    protected virtual IImmutableList<(object id, object document)> GivenDocuments()
    {
        return ImmutableList<(object id, object document)>.Empty;
    }

    [PublicAPI]
    protected virtual IImmutableList<(string projectionName, long? position)> GivenPositions()
    {
        return ImmutableList<(string projectionName, long? position)>.Empty;
    }

    [PublicAPI]
    public record GivenConfiguration(
        IImmutableList<(object id, object document)> InitialDocuments,
        IImmutableList<(string projectionName, long? position)> InitialPositions)
    {
        public static GivenConfiguration Empty => new(
            ImmutableList<(object id, object document)>.Empty,
            ImmutableList<(string projectionName, long? position)>.Empty);

        public GivenConfiguration WithInitialDocument(object id, object document)
        {
            return this with
            {
                InitialDocuments = InitialDocuments.Add((id, document))
            };
        }

        public GivenConfiguration WithInitialPosition(string projectionName, long? position)
        {
            return this with
            {
                InitialPositions = InitialPositions.Add((projectionName, position))
            };
        }
    }
}