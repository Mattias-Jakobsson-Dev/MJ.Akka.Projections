using System.Collections.Immutable;
using System.Diagnostics;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionSequencerTests;

public abstract class ProjectionSequencerBaseFixture : TestKit, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var sequencer = ProjectionSequencer<string, TestDocument<string>>.Create(
            Sys,
            new ProjectionConfiguration<string, TestDocument<string>>(
                new TestProjection<string>(ImmutableList<object>.Empty),
                new InMemoryProjectionStorage(),
                new InMemoryPositionStorage(),
                new TestProjectionFactory(),
                null,
                BatchEventBatchingStrategy.Default,
                BatchWithinEventPositionBatchingStrategy.Default,
                new FakeEventHandler()),
            CancellationToken.None);

        var batches = SetupBatches();

        var position = 1;

        var responses = new Dictionary<string, Task<Messages.IProjectEventsResponse>>();

        var startedAt = Stopwatch.StartNew();
        
        foreach (var batch in batches.OrderBy(x => x.Value.sortOrder))
        {
            var currentPosition = position;
            
            var events = batch.Value.delays.Select((delay, index) =>
                    new EventWithPosition(
                        new Events.DelayProcessingEvent(batch.Value.documentId, delay, startedAt),
                        currentPosition + index))
                .ToImmutableList();
            
            var response = await sequencer
                .Ask<ProjectionSequencer<string, TestDocument<string>>.Responses.StartProjectingResponse>(
                    new ProjectionSequencer<string, TestDocument<string>>.Commands.StartProjecting(
                        events));

            responses[batch.Key] = response.Tasks[0].task;

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
        public void Reset()
        {
            
        }

        public Task<IProjectorProxy> GetProjector<TId, TDocument>(
            TId id,
            ProjectionConfiguration configuration)
            where TId : notnull where TDocument : notnull
        {
            return Task.FromResult<IProjectorProxy>(new TestProjectionProxy());
        }

        private class TestProjectionProxy : IProjectorProxy
        {
            public async Task<Messages.IProjectEventsResponse> ProjectEvents(
                IImmutableList<EventWithPosition> events,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                var delayEvent = events
                    .Select(x => x.Event)
                    .OfType<Events.DelayProcessingEvent>()
                    .First();

                var timeSinceStarted = delayEvent.SinceCreated.Elapsed;

                await Task.Delay(delayEvent.Delay, cancellationToken);

                return new AckWithTime(
                    timeSinceStarted,
                    delayEvent.SinceCreated.Elapsed,
                    events.GetHighestEventNumber());
            }

            public void StopAllInProgress()
            {
                
            }
        }
    }

    public record AckWithTime(TimeSpan TimeSinceStarted, TimeSpan TimeSinceCompleted, long? Position) 
        : Messages.Acknowledge(Position);

    private static class Events
    {
        public record DelayProcessingEvent(string DocumentId, TimeSpan Delay, Stopwatch SinceCreated);
    }
    
    private class FakeEventHandler : IHandleEventInProjection<TestDocument<string>>
    {
        public IImmutableList<object> Transform(object evnt)
        {
            return ImmutableList.Create(evnt);
        }

        public DocumentId GetDocumentIdFrom(object evnt)
        {
            if (evnt is Events.DelayProcessingEvent delayEvent)
                return new DocumentId(delayEvent.DocumentId, true);

            return new DocumentId(null, false);
        }

        public Task<(TestDocument<string>? document, bool hasHandler)> Handle(
            TestDocument<string>? document,
            object evnt,
            long position,
            CancellationToken cancellationToken)
        {
            return Task.FromResult((document, false));
        }
    }
}