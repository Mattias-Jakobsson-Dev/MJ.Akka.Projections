using System.Collections.Immutable;
using Akka.Streams;
using Akka.TestKit.Xunit2;
using AutoFixture;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

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
                .WithPositionStorage(new SequentialPositionStorageTester()));
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
                .WithPositionStorage(new SequentialPositionStorageTester())
                .WithBatchedStorage(1, new BatchSizeStorageBatchingStrategy(100)));
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
            x => x.WithRestartSettings(null));
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
            x => x
                .WithRestartSettings(null)
                .WithBatchedStorage(1, new BatchSizeStorageBatchingStrategy(100)));
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
                    TimeSpan.FromMilliseconds(50)))
                .WithRestartSettings(null));
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
                    TimeSpan.FromMilliseconds(50)))
                .WithRestartSettings(null)
                .WithBatchedStorage(1, new BatchSizeStorageBatchingStrategy(100)));
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
                .WithRestartSettings(null));
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
                .WithRestartSettings(null)
                .WithBatchedStorage(1, new BatchSizeStorageBatchingStrategy(100)));
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
            x => x
                .WithModifiedStorage(new RandomFailureStorageWrapper.Modifier(failurePercentage)));
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
            x => x
                .WithModifiedStorage(new RandomFailureStorageWrapper.Modifier(failurePercentage))
                .WithBatchedStorage());
    }

    private async Task RunTest(
        int numberOfDocuments,
        int numberOfEvents,
        int failurePercentage,
        Func<
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>>,
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>>> configure)
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

        var projection = new TestProjection<string>(
            events.OfType<object>().ToImmutableList(), 
            ImmutableList<StorageFailures>.Empty);

        var storageSetup = new SetupInMemoryStorage();
        
        var loader = projection
            .GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();
        var eventTracker = new TrackEventsStorageWrapper.Modifier();
        
        var coordinator = await Sys
            .Projections(config => configure(config
                    .WithRestartSettings(RestartSettings.Create(TimeSpan.Zero, TimeSpan.Zero, 1))
                    .WithProjection(projection)
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithEventBatchingStrategy(new NoEventBatchingStrategy(100)))
                .WithModifiedStorage(storageWrapper)
                .WithModifiedStorage(eventTracker),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(TimeSpan.FromSeconds(30));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.Should().Be(numberOfEvents);

        foreach (var documentId in documentIds)
        {
            var context = await loader.Load(documentId, projection.GetDefaultContext);

            context.Exists().Should().BeTrue();

            var documentEvents = events
                .Where(x => x.DocId.ToString() == context.Id.ToString())
                .ToImmutableList();

            context.Document!.HandledEvents.Should().HaveCount(documentEvents.Count);

            context.Document!.HandledEvents.Should().BeEquivalentTo(documentEvents.Select(x => x.EventId));
        }

        projection.HandledEvents.Should().HaveCount(numberOfEvents);

        eventTracker.StoredEvents.Distinct().Should().HaveCount(numberOfEvents);
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