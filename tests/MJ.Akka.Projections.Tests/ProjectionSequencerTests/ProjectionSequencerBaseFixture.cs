using System.Collections.Immutable;
using System.Diagnostics;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Storage.Messages;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ProjectionSequencerTests;

public abstract class ProjectionSequencerBaseFixture : TestKit, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var projection = new TestProjection<string>(
            ImmutableList<object>.Empty, 
            ImmutableList<StorageFailures>.Empty);

        var storageSetup = new SetupInMemoryStorage();
        
        var sequencer = ProjectionSequencer.Create(
            Sys,
            new ProjectionConfiguration<
                string, 
                InMemoryProjectionContext<string, TestDocument<string>>, 
                SetupInMemoryStorage>(
                projection,
                storageSetup.CreateProjectionStorage(),
                projection.GetLoadProjectionContext(storageSetup),
                new InMemoryPositionStorage(),
                new TestProjectionFactory(),
                null,
                BatchEventBatchingStrategy.Default,
                BatchWithinEventPositionBatchingStrategy.Default,
                new FakeEventHandler()));
        
        sequencer.Reset(CancellationToken.None);

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
                .Ref
                .Ask<ProjectionSequencer.Responses.StartProjectingResponse>(
                    new ProjectionSequencer.Commands.StartProjecting(events));

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

    public class TestProjectionFactory : IKeepTrackOfProjectors
    {
        public Task<IProjectorProxy> GetProjector(
            object id,
            ProjectionConfiguration configuration)
        {
            return Task.FromResult<IProjectorProxy>(new TestProjectionProxy());
        }

        public IKeepTrackOfProjectors Reset()
        {
            return new TestProjectionFactory();
        }

        private class TestProjectionProxy : IProjectorProxy
        {
            public async Task<Messages.IProjectEventsResponse> ProjectEvents(
                ImmutableList<EventWithPosition> events,
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

            public Task StopAllInProgress(TimeSpan timeout)
            {
                return Task.CompletedTask;
            }
        }
    }

    public record AckWithTime(TimeSpan TimeSinceStarted, TimeSpan TimeSinceCompleted, long? Position) 
        : Messages.Acknowledge(Position);

    public static class Events
    {
        public record DelayProcessingEvent(string DocumentId, TimeSpan Delay, Stopwatch SinceCreated);
    }
    
    public class FakeEventHandler : IHandleEventInProjection<string, InMemoryProjectionContext<string, TestDocument<string>>>
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

        public Task<(bool handled, IImmutableList<IProjectionResult> results)> Handle(
            InMemoryProjectionContext<string, TestDocument<string>> context, 
            object evnt,
            long position, 
            CancellationToken cancellationToken)
        {
            return Task.FromResult<(bool handled, IImmutableList<IProjectionResult> results)>(
                (false, ImmutableList<IProjectionResult>.Empty));
        }
    }
}