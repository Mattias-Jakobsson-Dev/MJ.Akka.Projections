using System.Collections.Immutable;
using System.Diagnostics;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using MJ.Akka.Projections;
using MJ.Akka.Projections.Storage;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ProjectionSequencerTests;

public class When_waiting_for_group_to_finish_before_its_finished(
    When_waiting_for_group_to_finish_before_its_finished.Fixture fixture)
    : IClassFixture<When_waiting_for_group_to_finish_before_its_finished.Fixture>
{
    [Fact]
    public void Then_task_should_be_finished_after_waiting()
    {
        fixture.TaskFinishedAfterWait.Should().BeTrue();
    }
    
    [Fact]
    public void Then_task_should_not_be_finished_before_waiting()
    {
        fixture.TaskFinishedBeforeWait.Should().BeFalse();
    }
    
    public class Fixture : TestKit, IAsyncLifetime
    {
        public bool TaskFinishedBeforeWait { get; private set; }
        public bool TaskFinishedAfterWait { get; private set; }
        
        public async Task InitializeAsync()
        {
            var id = Guid.NewGuid().ToString();
            
            var sequencer = ProjectionSequencer<string, TestDocument<string>>.Create(
                Sys,
                new ProjectionConfiguration<string, TestDocument<string>>(
                    new TestProjection<string>(ImmutableList<object>.Empty),
                    new InMemoryProjectionStorage(),
                    new InMemoryPositionStorage(),
                    new ProjectionSequencerBaseFixture.TestProjectionFactory(),
                    null,
                    BatchEventBatchingStrategy.Default,
                    BatchWithinEventPositionBatchingStrategy.Default,
                    new ProjectionSequencerBaseFixture.FakeEventHandler()));
        
            sequencer.Reset(CancellationToken.None);

            var startedAt = Stopwatch.StartNew();
            
            var events = ImmutableList.Create(
                new EventWithPosition(
                    new ProjectionSequencerBaseFixture.Events.DelayProcessingEvent(
                        id,
                        TimeSpan.FromMilliseconds(200),
                        startedAt),
                    1));
            
            var startTasksResponse = await sequencer
                .Ref
                .Ask<ProjectionSequencer<string, TestDocument<string>>.Responses.StartProjectingResponse>(
                    new ProjectionSequencer<string, TestDocument<string>>.Commands.StartProjecting(events));
            
            TaskFinishedBeforeWait = startTasksResponse.Tasks[0].task.IsCompletedSuccessfully;

            await sequencer
                .Ref
                .Ask<ProjectionSequencer<string, TestDocument<string>>.Responses.WaitForGroupToFinishResponse>(
                    new ProjectionSequencer<string, TestDocument<string>>.Commands.WaitForGroupToFinish(
                        startTasksResponse.Tasks[0].groupId,
                        new PositionData(1)));

            TaskFinishedAfterWait = startTasksResponse.Tasks[0].task.IsCompletedSuccessfully;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}