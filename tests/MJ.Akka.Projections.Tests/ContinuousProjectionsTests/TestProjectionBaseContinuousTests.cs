using System.Collections.Immutable;
using Akka.Streams;
using AutoFixture;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public abstract class TestProjectionBaseContinuousTests<TId>(IHaveActorSystem actorSystemHandler)
    : BaseContinuousProjectionsTests<TId, InMemoryProjectionContext<TId, TestDocument<TId>>, SetupInMemoryStorage>(
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
                (item is DocumentResults.DocumentModified store && ((TestDocument<TId>)store.Document).PreviousEventFailures.Any()) ||
                item is DocumentResults.DocumentCreated created && ((TestDocument<TId>)created.Document).PreviousEventFailures.Any(),
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

        position.Should().Be(5);

        var firstContext = await loader.Load(firstDocumentId);

        firstContext.Exists().Should().BeTrue();

        firstContext.Document!.HandledEvents.Should().HaveCount(3);

        var secondContext = await loader.Load(secondDocumentId);

        secondContext.Exists().Should().BeTrue();

        secondContext.Document!.HandledEvents.Should().HaveCount(2);
    }
    
    protected override SetupInMemoryStorage CreateStorageSetup()
    {
        return new SetupInMemoryStorage();
    }

    protected override IProjection<TId, InMemoryProjectionContext<TId, TestDocument<TId>>, SetupInMemoryStorage> 
        GetProjection(
            IImmutableList<object> events,
            IImmutableList<StorageFailures> storageFailures,
            long? initialPosition = null)
    {
        return new TestProjection<TId>(events, storageFailures, initialPosition: initialPosition);
    }

    protected override object GetEventThatFails(TId id, int numberOfFailures)
    {
        return new Events<TId>.FailProjection(
            id,
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            numberOfFailures,
            new Exception("Projection failed"));
    }

    protected override object GetTestEvent(TId documentId)
    {
        return new Events<TId>.FirstEvent(documentId, Fixture.Create<string>());
    }

    protected override object GetTransformationEvent(TId documentId, IImmutableList<object> transformTo)
    {
        return new Events<TId>.TransformToMultipleEvents(transformTo.OfType<Events<TId>.IEvent>().ToImmutableList());
    }

    protected override object GetUnMatchedEvent(TId documentId)
    {
        return new Events<TId>.UnHandledEvent(documentId);
    }

    protected override object GetEventThatIsFilteredOut(TId documentId)
    {
        return new Events<TId>.EventWithFilter(documentId, Fixture.Create<string>(), () => false);
    }

    protected override Task VerifyContext(
        TId documentId,
        InMemoryProjectionContext<TId, TestDocument<TId>> context,
        IImmutableList<object> events,
        IProjection projection)
    {
        var projectedEvents = events
            .SelectMany(x =>
            {
                if (x is Events<TId>.TransformToMultipleEvents transform)
                    return transform.Events;

                return ImmutableList.Create((Events<TId>.IEvent)x);
            })
            .ToImmutableList();

        var eventsToCheck = projectedEvents
            .Where(x => x.DocId.ToString() == documentId.ToString())
            .ToImmutableList();

        context.Document!.HandledEvents.Count.Should().Be(eventsToCheck.Count);

        var position = 1;

        foreach (var evnt in eventsToCheck)
        {
            context.Document!.HandledEvents.Should().Contain(evnt.EventId);
            context.Document!.EventHandledOrder[evnt.EventId].Should().Be(position);

            position++;
        }

        var testProjection = (TestProjection<TId>)projection;

        testProjection.HandledEvents.Should().HaveCount(projectedEvents.Count);

        return Task.CompletedTask;
    }
}