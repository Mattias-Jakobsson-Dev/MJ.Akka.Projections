using System.Collections.Immutable;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionSequencers;

public class When_projecting_events_to_two_different_ids(When_projecting_events_to_two_different_ids.Fixture fixture)
    : IClassFixture<When_projecting_events_to_two_different_ids.Fixture>
{
    [Fact]
    public void Then_first_task_should_finish()
    {
        fixture.FirstTaskResponse.Should().NotBeNull();
    }

    [Fact]
    public void Then_second_task_should_finish()
    {
        fixture.SecondTaskResponse.Should().NotBeNull();
    }

    [Fact]
    public void Then_tasks_should_run_in_parallel()
    {
        fixture
            .FirstTaskResponse!
            .CompletedAt
            .Should()
            .BeCloseTo(fixture.SecondTaskResponse!.CompletedAt, TimeSpan.FromMilliseconds(100));
    }
    
    [PublicAPI]
    public class Fixture : ProjectionSequencerBaseFixture
    {
        public AckWithTime? FirstTaskResponse { get; private set; }
        public AckWithTime? SecondTaskResponse { get; private set; }
        
        protected override IImmutableDictionary<
            string, 
            (string documentId, int sortOrder, ImmutableList<TimeSpan> delays)> SetupBatches()
        {
            return new Dictionary<string, (string, int, ImmutableList<TimeSpan>)>
            {
                ["first"] = (Guid.NewGuid().ToString(), 1, ImmutableList.Create(TimeSpan.FromMilliseconds(200))),
                ["second"] = (Guid.NewGuid().ToString(), 2, ImmutableList.Create(TimeSpan.FromMilliseconds(200)))
            }.ToImmutableDictionary();
        }

        protected override Task FinishSetup(Func<string, AckWithTime?> getCompletionTime)
        {
            FirstTaskResponse = getCompletionTime("first");
            SecondTaskResponse = getCompletionTime("second");

            return Task.CompletedTask;
        }
    }
}