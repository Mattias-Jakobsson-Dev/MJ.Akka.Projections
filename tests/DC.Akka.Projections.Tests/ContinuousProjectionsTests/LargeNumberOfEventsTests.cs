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
        Func<IHaveConfiguration<ProjectionSystemConfiguration>, IHaveConfiguration<ProjectionSystemConfiguration>>
            configure,
        IProjectionStorage projectionStorage)
    {
        var documentIds = Enumerable
            .Range(0, numberOfDocuments)
            .Select(_ => _fixture.Create<string>())
            .ToImmutableList();

        var events = Enumerable
            .Range(1, numberOfEvents)
            .Select(x =>
            {
                var documentId = documentIds[x % numberOfDocuments];

                if (failurePercentage > 0 && _random.Next(100) <= failurePercentage)
                {
                    return (Events<string>.IEvent)new Events<string>.FailProjection(
                        documentId,
                        _fixture.Create<string>(),
                        _fixture.Create<string>(),
                        1,
                        new Exception("Failed projection"),
                        true);
                }

                return new Events<string>.FirstEvent(documentId, _fixture.Create<string>(), true);
            })
            .ToImmutableList();

        var projection = new TestProjection<string>(events.OfType<object>().ToImmutableList());

        IProjectionPositionStorage positionStorage = null!;

        var coordinator = await Sys
            .Projections(config => configure(config
                    .WithRestartSettings(RestartSettings.Create(TimeSpan.Zero, TimeSpan.Zero, 1))
                    .WithProjection(projection)
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithProjectionStorage(projectionStorage)
                    .WithEventBatchingStrategy(new NoEventBatchingStrategy(100)))
                .WithModifiedConfig(conf =>
                {
                    positionStorage = conf.PositionStorage!;

                    return conf;
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
}