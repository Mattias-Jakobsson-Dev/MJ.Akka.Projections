using System.Collections.Immutable;
using System.Diagnostics;
using Akka.Actor;
using Shouldly;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ProjectionSequencerTests;

/// <summary>
/// When a single StartProjecting batch contains more unique IDs than the configured parallelism,
/// the sequencer must not dispatch more than parallelism concurrent Run() calls at once.
/// Tasks that exceed the limit must be deferred and only start once an earlier task frees its slot.
/// </summary>
public class When_concurrency_limit_is_respected_within_a_single_batch(
    When_concurrency_limit_is_respected_within_a_single_batch.Fixture fixture)
    : IClassFixture<When_concurrency_limit_is_respected_within_a_single_batch.Fixture>
{
    // With parallelism=2 and 4 unique IDs, the batch is split into two chunks of 2.
    // Chunk1 (IDs 1 & 2) starts immediately; chunk2 (IDs 3 & 4) must wait.

    [Fact]
    public void Then_all_four_tasks_should_complete()
    {
        fixture.Chunk1Responses.Count.ShouldBe(2);
        fixture.Chunk2Responses.Count.ShouldBe(2);
    }

    [Fact]
    public void Then_chunk2_tasks_should_not_start_before_chunk1_tasks_finish()
    {
        // Each chunk1 task takes ~200ms. Chunk2 tasks should only start once a
        // chunk1 slot is freed, so their TimeSinceStarted must be >= the first
        // chunk1 completion minus a small tolerance.
        var earliestChunk1Completion = fixture.Chunk1Responses
            .Min(r => r.TimeSinceCompleted);

        var earliestChunk2Start = fixture.Chunk2Responses
            .Min(r => r.TimeSinceStarted);

        // chunk2 should start no earlier than chunk1 starts finishing.
        // Allow 50 ms tolerance for scheduling jitter.
        var tolerance = TimeSpan.FromMilliseconds(50);
        earliestChunk2Start.ShouldBeGreaterThan(earliestChunk1Completion - tolerance);
    }

    [Fact]
    public void Then_chunk1_tasks_should_start_before_chunk2_tasks()
    {
        var latestChunk1Start = fixture.Chunk1Responses
            .Max(r => r.TimeSinceStarted);
        var earliestChunk2Start = fixture.Chunk2Responses
            .Min(r => r.TimeSinceStarted);

        // Chunk2 tasks must start strictly later than chunk1 tasks.
        earliestChunk2Start.ShouldBeGreaterThan(latestChunk1Start);
    }

    public class Fixture : global::Akka.TestKit.Xunit2.TestKit, IAsyncLifetime
    {
        // Parallelism = 2 → max 2 concurrent Run() calls; 4 events → 2 chunks of 2.
        private const int Parallelism = 2;
        private static readonly TimeSpan TaskDelay = TimeSpan.FromMilliseconds(200);

        public IReadOnlyList<ProjectionSequencerBaseFixture.AckWithTime> Chunk1Responses { get; private set; } = [];
        public IReadOnlyList<ProjectionSequencerBaseFixture.AckWithTime> Chunk2Responses { get; private set; } = [];

        public async Task InitializeAsync()
        {
            var projection = new TestProjection<string>(
                ImmutableList<object>.Empty,
                ImmutableList<StorageFailures>.Empty);

            var storageSetup = new SetupInMemoryStorage();

            var sequencer = ProjectionSequencer.Create(
                Sys,
                new ProjectionConfiguration<
                    SimpleIdContext<string>,
                    InMemoryProjectionContext<string, TestDocument<string>>,
                    SetupInMemoryStorage>(
                    projection,
                    storageSetup.CreateProjectionStorage(),
                    projection.GetLoadProjectionContext(storageSetup),
                    new InMemoryPositionStorage(),
                    storageSetup.CreateStashStorage(),
                    new ProjectionSequencerBaseFixture.TestProjectionFactory(),
                    null,
                    new BatchEventBatchingStrategy(Parallelism),
                    BatchWithinEventPositionBatchingStrategy.Default,
                    new ProjectionSequencerBaseFixture.FakeEventHandler()));

            sequencer.Reset(CancellationToken.None);

            var startedAt = Stopwatch.StartNew();

            // Four unique document IDs – all with the same delay so the only variable
            // is when each task is *allowed* to start.
            var events = Enumerable.Range(1, 4)
                .Select(i => new EventWithPosition(
                    new ProjectionSequencerBaseFixture.Events.DelayProcessingEvent(
                        $"doc-{i}",
                        TaskDelay,
                        startedAt),
                    i))
                .ToImmutableList();

            var response = await sequencer.Ref
                .Ask<ProjectionSequencer.Responses.StartProjectingResponse>(
                    new ProjectionSequencer.Commands.StartProjecting(events));

            // Wait for all tasks to complete.
            var results = await Task.WhenAll(
                response.Tasks.Select(t => t.task));

            // The first Parallelism tasks belong to chunk1; the rest to chunk2.
            Chunk1Responses = results
                .Take(Parallelism)
                .Cast<ProjectionSequencerBaseFixture.AckWithTime>()
                .ToList();

            Chunk2Responses = results
                .Skip(Parallelism)
                .Cast<ProjectionSequencerBaseFixture.AckWithTime>()
                .ToList();
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}