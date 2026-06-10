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
/// When a single StartProjecting batch produces multiple chunks (because the number of unique
/// IDs exceeds the chunk/parallelism size), each chunk's group should be tracked independently.
/// WaitForGroupToFinish for chunk 2's group must resolve as soon as chunk 2's own tasks are
/// done – it must NOT wait for chunk 1's tasks to finish as well.
/// </summary>
public class When_group_in_multi_chunk_batch_completes_independently_of_other_chunks(
    When_group_in_multi_chunk_batch_completes_independently_of_other_chunks.Fixture fixture)
    : IClassFixture<When_group_in_multi_chunk_batch_completes_independently_of_other_chunks.Fixture>
{
    // Setup:
    //   Parallelism = 2  →  chunks of 2 tasks each.
    //   Chunk1 contains a fast task (doc-A ≈ 0 ms) and a SLOW task (doc-B = 500 ms).
    //   Chunk2 contains two fast tasks (doc-C = 50 ms, doc-D = 50 ms).
    //
    // With the concurrency limit: doc-A and doc-B start immediately. Once doc-A frees a
    // slot, doc-C starts; once doc-C frees a slot, doc-D starts. doc-B keeps its slot for
    // ~500 ms.
    //
    // Group2 tracks only doc-C and doc-D (chunk2). It should be signalled when doc-D
    // completes (~100 ms after the first slot opens), long before doc-B finishes at ~500 ms.
    //
    // If group2 mistakenly included doc-B (the old bug), WaitForGroupToFinish(group2)
    // would not respond until ~500 ms.

    [Fact]
    public void Then_all_tasks_should_complete()
    {
        fixture.AllTasksCompleted.ShouldBeTrue();
    }

    [Fact]
    public void Then_group2_wait_should_resolve_before_the_slow_chunk1_task_finishes()
    {
        // WaitForGroupToFinish(group2) responded at this many ms after the test started.
        // The slow chunk1 task (doc-B) completed at this many ms after the test started.
        // Group2 must have been signalled first.
        fixture.Group2WaitResponseElapsed.ShouldBeLessThan(fixture.SlowChunk1TaskCompletedElapsed);
    }

    [Fact]
    public void Then_group2_wait_should_respond_after_chunk2_tasks_finish()
    {
        // Sanity check: the group2 response must not arrive before chunk2 tasks complete.
        fixture.Group2WaitResponseElapsed.ShouldBeGreaterThanOrEqualTo(
            fixture.Chunk2TasksCompletedElapsed - TimeSpan.FromMilliseconds(50));
    }

    public class Fixture : global::Akka.TestKit.Xunit2.TestKit, IAsyncLifetime
    {
        private const int Parallelism = 2;

        private static readonly TimeSpan FastDelay = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan SlowDelay = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan NearZeroDelay = TimeSpan.Zero;

        public bool AllTasksCompleted { get; private set; }

        /// <summary>Elapsed time from test start until WaitForGroupToFinish(group2) responded.</summary>
        public TimeSpan Group2WaitResponseElapsed { get; private set; }

        /// <summary>Elapsed time from test start until the slow chunk1 task (doc-B) completed.</summary>
        public TimeSpan SlowChunk1TaskCompletedElapsed { get; private set; }

        /// <summary>Elapsed time from test start until ALL chunk2 tasks completed.</summary>
        public TimeSpan Chunk2TasksCompletedElapsed { get; private set; }

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

            var testTimer = Stopwatch.StartNew();

            // Chunk1: doc-A (near-zero delay) + doc-B (slow delay).
            // Chunk2: doc-C (fast) + doc-D (fast).
            var events = new[]
                {
                    ("doc-A", NearZeroDelay),
                    ("doc-B", SlowDelay),
                    ("doc-C", FastDelay),
                    ("doc-D", FastDelay),
                }.Select((entry, i) => new EventWithPosition(
                    new ProjectionSequencerBaseFixture.Events.DelayProcessingEvent(
                        entry.Item1,
                        entry.Item2,
                        testTimer),
                    i + 1))
                .ToImmutableList();

            var response = await sequencer.Ref
                .Ask<ProjectionSequencer.Responses.StartProjectingResponse>(
                    new ProjectionSequencer.Commands.StartProjecting(events));

            // Identify the two groups. All tasks in the same chunk share a groupId.
            var group1Id = response.Tasks[0].groupId;
            var group2Id = response.Tasks.First(t => t.groupId != group1Id).groupId;

            // Isolate the individual tasks by doc id (by index in the response).
            // Index 0 = doc-A (fast, chunk1), index 1 = doc-B (slow, chunk1),
            // index 2 = doc-C (fast, chunk2), index 3 = doc-D (fast, chunk2).
            var taskA = response.Tasks[0].task;
            var taskB = response.Tasks[1].task;
            var taskC = response.Tasks[2].task;
            var taskD = response.Tasks[3].task;

            // Concurrently:
            //   - send WaitForGroupToFinish for group2
            //   - wait for all tasks so we capture completion times
            var waitForGroup2Task = sequencer.Ref
                .Ask<ProjectionSequencer.Responses.WaitForGroupToFinishResponse>(
                    new ProjectionSequencer.Commands.WaitForGroupToFinish(
                        group2Id,
                        new PositionData(3)));

            var allTasksTask = Task.WhenAll(taskA, taskB, taskC, taskD);

            // WaitForGroupToFinish(group2) should resolve when chunk2 tasks are done –
            // without waiting for the slow chunk1 task (doc-B).
            await waitForGroup2Task;
            Group2WaitResponseElapsed = testTimer.Elapsed;

            // Capture when chunk2 tasks finished (should be ≤ group2 wait response time).
            var chunk2Results = await Task.WhenAll(taskC, taskD);
            Chunk2TasksCompletedElapsed = chunk2Results
                .Cast<ProjectionSequencerBaseFixture.AckWithTime>()
                .Max(r => r.TimeSinceCompleted);

            // Wait for everything, then capture the slow task's completion time.
            await allTasksTask;
            AllTasksCompleted = true;

            var bResult = (ProjectionSequencerBaseFixture.AckWithTime)await taskB;
            SlowChunk1TaskCompletedElapsed = bResult.TimeSinceCompleted;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}