using System.Collections.Immutable;
using Akka.Streams;
using AutoFixture;
using Shouldly;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public abstract class TestProjectionBaseContinuousTests<TId>(IHaveActorSystem actorSystemHandler)
    : BaseContinuousProjectionsTests<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TestDocument<TId>>, SetupInMemoryStorage>(
        actorSystemHandler) 
    where TId : notnull
{
    private readonly IHaveActorSystem _actorSystemHandler = actorSystemHandler;
    
    protected virtual TimeSpan ProjectionWaitTime { get; } = TimeSpan.FromSeconds(5);
    
    [Fact]
    public async Task
        Projecting_event_that_fails_once_and_then_storage_fails_once_for_same_event_with_restart_behaviour()
    {
        using var system = _actorSystemHandler.StartNewActorSystem();

        var firstDocumentId = Fixture.Create<TId>();
        var secondDocumentId = Fixture.Create<TId>();

        var events = ImmutableList.Create(
            GetTestEvent(firstDocumentId),
            GetTestEvent(secondDocumentId),
            GetEventThatFails(firstDocumentId, 1),
            GetTestEvent(firstDocumentId),
            GetTestEvent(secondDocumentId));

        var failures = ImmutableList.Create(
            new StorageFailures(item =>
                item is ContextWithDocument<SimpleIdContext<TId>, TestDocument<TId>> { Document: not null } store && store.Document.PreviousEventFailures.Any(),
            _ => false,
            new Exception("Failure")));

        var storageSetup = new SetupInMemoryStorage();
        
        var projection = GetProjection(events, failures);
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                    .WithProjection(projection))
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(100))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(ProjectionWaitTime);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(5);

        var firstContext = await loader.Load(firstDocumentId, projection.GetDefaultContext);

        firstContext.Exists().ShouldBeTrue();

        firstContext.Document!.HandledEvents.Count.ShouldBe(3);

        var secondContext = await loader.Load(secondDocumentId, projection.GetDefaultContext);

        secondContext.Exists().ShouldBeTrue();

        secondContext.Document!.HandledEvents.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task Stopping_projection_mid_flight_cancels_long_running_handler()
    {
        using var system = _actorSystemHandler.StartNewActorSystem();

        var documentId = Fixture.Create<TId>();

        // Handler delays for 30 s — far longer than the test timeout — but respects the CancellationToken.
        var events = ImmutableList.Create<object>(
            new Events<TId>.DelayHandlingWithCancellationToken(
                documentId,
                Fixture.Create<string>(),
                TimeSpan.FromSeconds(30)));

        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        var proxy = coordinator.Get(projection.Name)!;

        // Give the handler a moment to start
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Stop should not hang forever — the cancellation token should interrupt the handler
        var stopTask = proxy.Stop();
        await stopTask.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Chained_handlers_for_same_event_execute_in_order()
    {
        using var system = _actorSystemHandler.StartNewActorSystem();

        var documentId = Fixture.Create<TId>();
        var eventId1 = Fixture.Create<string>();
        var eventId2 = Fixture.Create<string>();

        // Two separate FirstEvent instances (different eventIds) for the same document —
        // verify they are both handled and in the correct order.
        var events = ImmutableList.Create<object>(
            new Events<TId>.FirstEvent(documentId, eventId1),
            new Events<TId>.FirstEvent(documentId, eventId2));

        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        var storageWrapper = new TestStorageWrapper.Modifier();
        var loader = projection.GetLoadProjectionContext(storageSetup);

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(ProjectionWaitTime);

        var context = await loader.Load(documentId, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();
        context.Document!.HandledEvents.Count.ShouldBe(2);
        context.Document!.EventHandledOrder[eventId1].ShouldBe(1);
        context.Document!.EventHandledOrder[eventId2].ShouldBe(2);
    }

    protected override SetupInMemoryStorage CreateStorageSetup()
    {
        return new SetupInMemoryStorage();
    }

    protected override IProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TestDocument<TId>>, SetupInMemoryStorage> 
        GetProjection(
            IImmutableList<object> events,
            IImmutableList<StorageFailures> storageFailures,
            long? initialPosition = null)
    {
        return new TestProjection<TId>(events, storageFailures, initialPosition: initialPosition);
    }

    protected override object GetEventThatFails(SimpleIdContext<TId> id, int numberOfFailures)
    {
        return new Events<TId>.FailProjection(
            id,
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            numberOfFailures,
            new Exception("Projection failed"));
    }

    protected override object GetTestEvent(SimpleIdContext<TId> documentId)
    {
        return new Events<TId>.FirstEvent(documentId, Fixture.Create<string>());
    }

    protected override object GetTransformationEvent(SimpleIdContext<TId> documentId, IImmutableList<object> transformTo)
    {
        return new Events<TId>.TransformToMultipleEvents(transformTo.OfType<Events<TId>.IEvent>().ToImmutableList());
    }

    protected override object GetUnMatchedEvent(SimpleIdContext<TId> documentId)
    {
        return new Events<TId>.UnHandledEvent(documentId);
    }

    protected override object GetEventThatIsFilteredOut(SimpleIdContext<TId> documentId)
    {
        return new Events<TId>.EventWithFilter(documentId, Fixture.Create<string>(), () => false);
    }

    protected override object GetEventThatDoesntGetDocumentId(SimpleIdContext<TId> documentId)
    {
        return new Events<TId>.EventThatDoesntGetDocumentId(documentId, Fixture.Create<string>());
    }

    protected override object GetEventWithDataForId(SimpleIdContext<TId> documentId, string data)
    {
        return new Events<TId>.EventWithDataId(documentId, Fixture.Create<string>(), data);
    }

    protected override object GetEventWithDataForHandler(SimpleIdContext<TId> documentId, string data)
    {
        return new Events<TId>.EventWithDataHandler(documentId, Fixture.Create<string>(), data);
    }

    protected override object GetEventWithDataForTransform(SimpleIdContext<TId> documentId, string data, IImmutableList<object> transformTo)
    {
        return new Events<TId>.EventWithDataTransform(documentId, Fixture.Create<string>(), data, transformTo.OfType<Events<TId>.IEvent>().ToImmutableList());
    }

    protected override Task VerifyDataContext(
        SimpleIdContext<TId> documentId,
        InMemoryProjectionContext<TId, TestDocument<TId>> context,
        string expectedData)
    {
        context.Document!.ReceivedData.ShouldContain(expectedData);
        return Task.CompletedTask;
    }

    protected override Task VerifyContext(
        SimpleIdContext<TId> documentId,
        InMemoryProjectionContext<TId, TestDocument<TId>> context,
        IImmutableList<object> events,
        IProjection projection)
    {
        var projectedEvents = events
            .SelectMany(x =>
            {
                if (x is Events<TId>.TransformToMultipleEvents transform)
                    return transform.Events;

                return x is Events<TId>.IEvent parsedEvent 
                    ? ImmutableList.Create(parsedEvent) 
                    : ImmutableList<Events<TId>.IEvent>.Empty;
            })
            .ToImmutableList();

        var eventsToCheck = projectedEvents
            .Where(x => x.DocId.ToString() == documentId.ToString())
            .ToImmutableList();

        context.Document!.HandledEvents.Count.ShouldBe(eventsToCheck.Count);

        var position = 1;

        foreach (var evnt in eventsToCheck)
        {
            context.Document!.HandledEvents.ShouldContain(evnt.EventId);
            context.Document!.EventHandledOrder[evnt.EventId].ShouldBe(position);

            position++;
        }

        var testProjection = (TestProjection<TId>)projection;

        testProjection.HandledEvents.Count.ShouldBe(projectedEvents.Count);

        return Task.CompletedTask;
    }
}