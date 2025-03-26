using System.Collections.Immutable;
using Akka.Streams;
using AutoFixture;
using MJ.Akka.Projections.Storage;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public abstract class TestProjectionBaseContinuousTests<TId>(IHaveActorSystem actorSystemHandler)
    : BaseContinuousProjectionsTests<TId, TestDocument<TId>>(actorSystemHandler) where TId : notnull
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
        
        var projection = GetProjection(events);
        IProjectionStorage projectionStorage = null!;
        IProjectionPositionStorage positionStorage = null!;

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
                .WithModifiedConfig(conf =>
                {
                    projectionStorage = new FailStorage(
                        conf.ProjectionStorage!,
                        ImmutableList.Create(new StorageFailures(
                            items => ((TestDocument<TId>)items
                                    .Document)
                                .PreviousEventFailures
                                .Any(),
                            _ => false,
                            _ => false,
                            new Exception("Failure"))));

                    positionStorage = conf.PositionStorage!;

                    return conf with
                    {
                        ProjectionStorage = projectionStorage
                    };
                }))
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(ProjectionWaitTime);

        var position = await positionStorage.LoadLatestPosition(projection.Name);

        position.Should().Be(5);

        var firstDocument = await projectionStorage.LoadDocument<TestDocument<TId>>(firstDocumentId);

        firstDocument.Should().NotBeNull();

        firstDocument!.HandledEvents.Should().HaveCount(3);

        var secondDocument = await projectionStorage.LoadDocument<TestDocument<TId>>(secondDocumentId);

        secondDocumentId.Should().NotBeNull();

        secondDocument!.HandledEvents.Should().HaveCount(2);
    }

    protected override IProjection<TId, TestDocument<TId>> GetProjection(IImmutableList<object> events)
    {
        return new TestProjection<TId>(events);
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

    protected override Task VerifyDocument(
        TId documentId,
        TestDocument<TId> document,
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

        document.HandledEvents.Count.Should().Be(eventsToCheck.Count);

        var position = 1;

        foreach (var evnt in eventsToCheck)
        {
            document.HandledEvents.Should().Contain(evnt.EventId);
            document.EventHandledOrder[evnt.EventId].Should().Be(position);

            position++;
        }

        var testProjection = (TestProjection<TId>)projection;

        testProjection.HandledEvents.Should().HaveCount(projectedEvents.Count);

        return Task.CompletedTask;
    }
}