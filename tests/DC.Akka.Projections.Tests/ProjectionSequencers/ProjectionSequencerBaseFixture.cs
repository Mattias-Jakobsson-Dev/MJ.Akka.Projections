using System.Collections.Immutable;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionSequencers;

public abstract class ProjectionSequencerBaseFixture : TestKit, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await Sys
            .Projections()
            .WithProjection(
                new TestProjection<string>(ImmutableList<object>.Empty),
                conf => conf
                    .WithProjectionFactory(new TestProjectionFactory()))
            .Start();

        var sequencer = ProjectionSequencer<string, TestDocument<string>>.Create(Sys);

        var batches = SetupBatches();

        var position = 1;

        var responses = new Dictionary<string, Task<Messages.IProjectEventsResponse>>();
        
        foreach (var batch in batches.OrderBy(x => x.Value.sortOrder))
        {
            var currentPosition = position;
            
            var events = batch.Value.delays.Select((delay, index) =>
                    new EventWithPosition(
                        new Events.DelayProcessingEvent(delay),
                        currentPosition + index))
                .ToImmutableList();
            
            var response = await sequencer
                .Ask<ProjectionSequencer<string, TestDocument<string>>.Responses.StartProjectingResponse>(
                    new ProjectionSequencer<string, TestDocument<string>>.Commands.StartProjecting(
                        new IHandleEventInProjection<string, TestDocument<string>>.DocumentIdResponse(
                            batch.Value.documentId,
                            true),
                        events));

            responses[batch.Key] = response.Task;

            position += batch.Value.delays.Count;
        }

        var finishedTasks = (await Task.WhenAll(
                responses
                    .Select(async x =>
                    {
                        var responseData = await x.Value;

                        return new
                        {
                            Id = x.Key,
                            Response = (AckWithTime)responseData
                        };
                    })))
            .ToImmutableDictionary(x => x.Id, x => x.Response);

        await FinishSetup(id => finishedTasks.GetValueOrDefault(id));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected abstract IImmutableDictionary<string, (string documentId, int sortOrder, ImmutableList<TimeSpan> delays)> 
        SetupBatches();

    protected abstract Task FinishSetup(Func<string, AckWithTime?> getCompletionTime);

    private class TestProjectionFactory : IKeepTrackOfProjectors
    {
        public Task<IProjectorProxy> GetProjector<TId, TDocument>(
            TId id,
            ProjectionConfiguration<TId, TDocument> configuration)
            where TId : notnull where TDocument : notnull
        {
            return Task.FromResult<IProjectorProxy>(new TestProjectionProxy());
        }

        private class TestProjectionProxy : IProjectorProxy
        {
            public async Task<Messages.IProjectEventsResponse> ProjectEvents(
                IImmutableList<EventWithPosition> events)
            {
                var startedAt = DateTimeOffset.Now;
                
                var delayTime = events
                    .Select(x => x.Event)
                    .OfType<Events.DelayProcessingEvent>()
                    .Select(x => x.Delay.Ticks)
                    .Sum(x => x);

                await Task.Delay(TimeSpan.FromTicks(delayTime));

                return new AckWithTime(startedAt, DateTimeOffset.Now);
            }
        }
    }

    public record AckWithTime(DateTimeOffset StartedAt, DateTimeOffset CompletedAt) : Messages.IProjectEventsResponse;

    private static class Events
    {
        public record DelayProcessingEvent(TimeSpan Delay);
    }
}