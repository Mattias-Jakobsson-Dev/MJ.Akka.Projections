using System.Collections.Immutable;
using AutoFixture;
using MJ.Akka.Projections;
using MJ.Akka.Projections.Tests.ContinuousProjectionsTests;
using FluentAssertions;
using MJ.Akka.Projections.Tests.TestData;

namespace MJ.Akka.Projections.Tests.OnTimeProjectionsTests;

public abstract class TestProjectionBaseOneTimeTests<TId>(IHaveActorSystem actorSystemHandler) 
    : BaseOneTimeProjectionsTest<TId, TestDocument<TId>>(actorSystemHandler) where TId : notnull
{
    protected override IProjection<TId, TestDocument<TId>> GetProjection(IImmutableList<object> events)
    {
        return new TestProjection<TId>(events);
    }

    protected override IProjection<TId, TestDocument<TId>> GetSecondaryProjection(IImmutableList<object> events)
    {
        return new TestProjection<TId>(events, $"SecondaryTestProjectionOf{typeof(TId).Name}");
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

    protected override Task VerifyDocument(TId documentId, TestDocument<TId> document, IImmutableList<object> events)
    {
        var eventsToCheck = events
            .SelectMany(x =>
            {
                if (x is Events<TId>.TransformToMultipleEvents transform)
                    return transform.Events;

                return ImmutableList.Create((Events<TId>.IEvent)x);
            })
            .Where(x => x.DocId.ToString() == documentId.ToString())
            .ToImmutableList();

        document.HandledEvents.Count.Should().Be(eventsToCheck.Count);

        foreach (var evnt in eventsToCheck)
        {
            document.HandledEvents.Should().Contain(evnt.EventId);
        }
        
        return Task.CompletedTask;
    }
}