using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;
using AutoFixture;
using FluentAssertions;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithComplexIdContextTests(NormalTestKitActorSystem actorSystemSetup)
    : BaseContinuousProjectionsTests<
        ProjectionWithComplexIdContextTests.ComplexIdContext,
        InMemoryProjectionContext<
            ProjectionWithComplexIdContextTests.ComplexIdContext,
            TestDocument<string>>,
        SetupInMemoryStorage>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
{
    protected override SetupInMemoryStorage CreateStorageSetup()
    {
        return new SetupInMemoryStorage();
    }
    
    protected override IProjection<
            ComplexIdContext,
            InMemoryProjectionContext<ComplexIdContext, TestDocument<string>>, 
            SetupInMemoryStorage> GetProjection(
            IImmutableList<object> events,
            IImmutableList<StorageFailures> storageFailures,
            long? initialPosition = null)
    {
        return new TestProjectionWithCustomIdContext<ComplexIdContext, string>(
            events,
            storageFailures,
            x => new ComplexIdContext(x, new ComplexChildObject("data")),
            initialPosition: initialPosition);
    }

    protected override object GetEventThatFails(ComplexIdContext id, int numberOfFailures)
    {
        return new Events<string>.FailProjection(
            id.GetStringRepresentation(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            numberOfFailures,
            new Exception("Projection failed"));
    }

    protected override object GetTestEvent(ComplexIdContext documentId)
    {
        return new Events<string>.FirstEvent(documentId, Fixture.Create<string>());
    }

    protected override object GetTransformationEvent(ComplexIdContext documentId, IImmutableList<object> transformTo)
    {
        return new Events<string>.TransformToMultipleEvents(transformTo.OfType<Events<string>.IEvent>().ToImmutableList());
    }

    protected override object GetUnMatchedEvent(ComplexIdContext documentId)
    {
        return new Events<string>.UnHandledEvent(documentId);
    }

    protected override object GetEventThatIsFilteredOut(ComplexIdContext documentId)
    {
        return new Events<string>.EventWithFilter(documentId, Fixture.Create<string>(), () => false);
    }
    
    protected override Task VerifyContext(
        ComplexIdContext documentId,
        InMemoryProjectionContext<ComplexIdContext, TestDocument<string>> context, 
        IImmutableList<object> events,
        IProjection projection)
    {
        var projectedEvents = events
            .SelectMany(x =>
            {
                if (x is Events<string>.TransformToMultipleEvents transform)
                    return transform.Events;
    
                return ImmutableList.Create((Events<string>.IEvent)x);
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
    
        var testProjection = (TestProjectionWithCustomIdContext<ComplexIdContext, string>)projection;
    
        testProjection.HandledEvents.Should().HaveCount(projectedEvents.Count);
    
        return Task.CompletedTask;
    }
    
    [PublicAPI]
    public record ComplexIdContext(string DocumentId, ComplexChildObject Child) : IProjectionIdContext
    {
        public virtual bool Equals(IProjectionIdContext? other)
        {
            return other is ComplexIdContext complexContext && DocumentId == complexContext.DocumentId;
        }

        public string GetStringRepresentation()
        {
            return DocumentId;
        }

        public override string ToString()
        {
            return GetStringRepresentation();
        }

        public static implicit operator string(ComplexIdContext context) => context.DocumentId;
    }

    [PublicAPI]
    public record ComplexChildObject(string Data);
}