using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Streams;
using Akka.TestKit.Xunit2;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.ContinuousProjectionsTests;

public class LargeNumberOfEventsTests : TestKit
{
    private readonly Random _random = new();
    private readonly Fixture _fixture = new();

    [Fact]
    public Task Handling_events_without_failures_with_normal_storage_should_store_positions_sequentially()
    {
        return RunTest(
            50,
            1_000,
            0,
            x => x
                .WithRestartSettings(null)
                .WithPositionStorage(new SequentialPositionStorageTester()),
            new InMemoryProjectionStorage());
    }

    [Fact]
    public Task Handling_events_without_failures_with_batched_storage_should_store_positions_sequentially()
    {
        return RunTest(
            50,
            1_000,
            0,
            x => x
                .WithRestartSettings(null)
                .WithPositionStorage(new SequentialPositionStorageTester()),
            new InMemoryProjectionStorage()
                .Batched(Sys, 1, new BatchSizeStorageBatchingStrategy(100)));
    }

    [Theory]
    [InlineData(50, 1_000)]
    [InlineData(100, 1_000)]
    public Task Handling_events_without_failures_with_normal_storage(
        int numberOfDocuments,
        int numberOfEvents)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            0,
            x => x.WithRestartSettings(null),
            new InMemoryProjectionStorage());
    }

    [Theory]
    [InlineData(50, 1_000)]
    [InlineData(100, 1_000)]
    public Task Handling_events_without_failures_with_batched_storage(
        int numberOfDocuments,
        int numberOfEvents)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            0,
            x => x.WithRestartSettings(null),
            new InMemoryProjectionStorage()
                .Batched(Sys, 1, new BatchSizeStorageBatchingStrategy(100)));
    }

    [Theory]
    [InlineData(50, 1_000)]
    [InlineData(100, 1_000)]
    [InlineData(1_000, 10_000)]
    public Task Handling_events_without_failures_with_normal_storage_and_batched_within_strategy(
        int numberOfDocuments,
        int numberOfEvents)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            0,
            x => x
                .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(
                    100,
                    TimeSpan.FromMilliseconds(50),
                    100))
                .WithRestartSettings(null),
            new InMemoryProjectionStorage());
    }

    [Theory]
    [InlineData(50, 1_000)]
    [InlineData(100, 1_000)]
    [InlineData(1_000, 10_000)]
    public Task Handling_events_without_failures_with_batched_storage_and_batched_within_strategy(
        int numberOfDocuments,
        int numberOfEvents)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            0,
            x => x
                .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(
                    100,
                    TimeSpan.FromMilliseconds(50),
                    100))
                .WithRestartSettings(null),
            new InMemoryProjectionStorage()
                .Batched(Sys, 1, new BatchSizeStorageBatchingStrategy(100)));
    }
    
    [Theory]
    [InlineData(50, 1_000)]
    [InlineData(100, 1_000)]
    [InlineData(1_000, 10_000)]
    public Task Handling_events_without_failures_with_normal_storage_and_no_batching_strategy(
        int numberOfDocuments,
        int numberOfEvents)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            0,
            x => x
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(100))
                .WithRestartSettings(null),
            new InMemoryProjectionStorage());
    }

    [Theory]
    [InlineData(50, 1_000)]
    [InlineData(100, 1_000)]
    [InlineData(1_000, 10_000)]
    public Task Handling_events_without_failures_with_batched_storage_and_no_batching_strategy(
        int numberOfDocuments,
        int numberOfEvents)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            0,
            x => x
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(100))
                .WithRestartSettings(null),
            new InMemoryProjectionStorage()
                .Batched(Sys, 1, new BatchSizeStorageBatchingStrategy(100)));
    }

    [Theory]
    [InlineData(50, 1_000, 1)]
    [InlineData(100, 1_000, 5)]
    public Task Handling_events_with_random_failures_with_restart_settings_and_normal_storage(
        int numberOfDocuments,
        int numberOfEvents,
        int failurePercentage)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            failurePercentage,
            x => x,
            new RandomFailureProjectionStorage(failurePercentage));
    }

    [Theory]
    [InlineData(10, 1_000, 1)]
    [InlineData(10, 1_000, 5)]
    public Task Handling_events_with_random_failures_with_restart_settings_and_batched_storage(
        int numberOfDocuments,
        int numberOfEvents,
        int failurePercentage)
    {
        return RunTest(
            numberOfDocuments,
            numberOfEvents,
            failurePercentage,
            x => x,
            new RandomFailureProjectionStorage(failurePercentage)
                .Batched(Sys, 1, new BatchSizeStorageBatchingStrategy(100)));
    }

    private async Task RunTest(
        int numberOfDocuments,
        int numberOfEvents,
        int failurePercentage,
        Func<
            IHaveConfiguration<ProjectionSystemConfiguration>,
            IHaveConfiguration<ProjectionSystemConfiguration>> configure,
        IProjectionStorage projectionStorage)
    {
        var documentIds = Enumerable
            .Range(0, numberOfDocuments)
            .Select(_ => _fixture.Create<string>())
            .ToImmutableList();

        var events = Enumerable
            .Range(1, numberOfEvents)
            .Select(Events<string>.IEvent (x) =>
            {
                var documentId = documentIds[x % numberOfDocuments];

                if (failurePercentage > 0 && _random.Next(100) <= failurePercentage)
                {
                    return new Events<string>.FailProjection(
                        documentId,
                        _fixture.Create<string>(),
                        _fixture.Create<string>(),
                        1,
                        new Exception("Failed projection"));
                }

                return new Events<string>.FirstEvent(documentId, _fixture.Create<string>());
            })
            .ToImmutableList();

        var projection = new TestProjection<string>(events.OfType<object>().ToImmutableList());

        IProjectionPositionStorage positionStorage = null!;
        ProjectionStorageWithEventStoredTracker usedProjectionStorageWithEventStoredTracker = null!;

        var coordinator = await Sys
            .Projections(config => configure(config
                    .WithRestartSettings(RestartSettings.Create(TimeSpan.Zero, TimeSpan.Zero, 1))
                    .WithProjection(projection)
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithProjectionStorage(projectionStorage)
                    .WithEventBatchingStrategy(new NoEventBatchingStrategy(100)))
                .WithModifiedConfig(conf =>
                {
                    usedProjectionStorageWithEventStoredTracker =
                        new ProjectionStorageWithEventStoredTracker(conf.ProjectionStorage!);

                    positionStorage = conf.PositionStorage!;

                    return conf with
                    {
                        ProjectionStorage = usedProjectionStorageWithEventStoredTracker
                    };
                }))
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(TimeSpan.FromSeconds(30));

        var position = await positionStorage.LoadLatestPosition(projection.Name);

        position.Should().Be(numberOfEvents);

        foreach (var documentId in documentIds)
        {
            var document = await projectionStorage.LoadDocument<TestDocument<string>>(documentId);

            document.Should().NotBeNull();

            var documentEvents = events
                .Where(x => x.DocId == document!.Id)
                .ToImmutableList();

            document!.HandledEvents.Should().HaveCount(documentEvents.Count);

            document.HandledEvents.Should().BeEquivalentTo(documentEvents.Select(x => x.EventId));
        }

        projection.HandledEvents.Should().HaveCount(numberOfEvents);

        usedProjectionStorageWithEventStoredTracker.StoredEvents.Distinct().Should().HaveCount(numberOfEvents);
    }

    private class RandomFailureProjectionStorage(int failurePercentage) : InMemoryProjectionStorage
    {
        private readonly Random _random = new();

        public override Task Store(
            IImmutableList<DocumentToStore> toUpsert,
            IImmutableList<DocumentToDelete> toDelete,
            CancellationToken cancellationToken = default)
        {
            if (_random.Next(100) <= failurePercentage)
                throw new Exception("Random failure");

            return base.Store(toUpsert, toDelete, cancellationToken);
        }
    }

    private class ProjectionStorageWithEventStoredTracker(IProjectionStorage innerStorage) : IProjectionStorage
    {
        public readonly ConcurrentBag<string> StoredEvents = [];

        public Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default)
        {
            return innerStorage.LoadDocument<TDocument>(id, cancellationToken);
        }

        public Task Store(
            IImmutableList<DocumentToStore> toUpsert,
            IImmutableList<DocumentToDelete> toDelete,
            CancellationToken cancellationToken = default)
        {
            var events = toUpsert
                .Select(x => x.Document as TestDocument<string>)
                .Where(x => x != null)
                .SelectMany(x => x!.HandledEvents)
                .ToImmutableList();

            foreach (var evnt in events)
            {
                StoredEvents.Add(evnt);
            }

            return innerStorage.Store(toUpsert, toDelete, cancellationToken);
        }
    }

    private class SequentialPositionStorageTester : InMemoryPositionStorage
    {
        private readonly SemaphoreSlim _lock = new(1);
        private int _storageCounter;

        public override async Task<long?> StoreLatestPosition(
            string projectionName,
            long? position,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _lock.WaitAsync(cancellationToken);

                var storedPosition = await base.StoreLatestPosition(
                    projectionName,
                    position,
                    cancellationToken);

                _storageCounter++;

                if (storedPosition != _storageCounter)
                    throw new Exception($"Storing position {storedPosition} when expecting {_storageCounter}");

                return storedPosition;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}