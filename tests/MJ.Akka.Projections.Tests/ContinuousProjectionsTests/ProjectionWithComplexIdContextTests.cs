using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;
using AutoFixture;
using Shouldly;
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

    protected override object GetEventThatDoesntGetDocumentId(ComplexIdContext documentId)
    {
        return new Events<string>.EventThatDoesntGetDocumentId(documentId, Fixture.Create<string>());
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
    
                return x is Events<string>.IEvent parsedEvent 
                    ? ImmutableList.Create(parsedEvent) 
                    : ImmutableList<Events<string>.IEvent>.Empty;
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
    
        var testProjection = (TestProjectionWithCustomIdContext<ComplexIdContext, string>)projection;
    
        testProjection.HandledEvents.Count.ShouldBe(projectedEvents.Count);
    
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

public class ComplexIdContextHandlerVerificationTests(NormalTestKitActorSystem actorSystemSetup)
    : IClassFixture<NormalTestKitActorSystem>
{
    private readonly Fixture _fixture = new();
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Verifies that the full ComplexIdContext (including child fields) is passed to every
    /// .HandleWith handler and every .ModifyDocument handler.
    /// </summary>
    [Fact]
    public async Task Complex_id_context_fields_are_passed_to_HandleWith_handler()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var event1 = new Events<string>.FirstEvent(docId, _fixture.Create<string>());
        var events = ImmutableList.Create<object>(event1);

        var projection = new ComplexIdContextVerifyingProjection(
            events,
            idContext);

        var storageSetup = new SetupInMemoryStorage();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        // Every handler invocation should have received the full context including child
        projection.ReceivedContexts.ShouldNotBeEmpty();
        foreach (var receivedContext in projection.ReceivedContexts)
        {
            receivedContext.DocumentId.ShouldBe(docId);
            receivedContext.Child.ShouldNotBeNull();
            receivedContext.Child.Data.ShouldBe(childData);
        }
    }

    [Fact]
    public async Task Complex_id_context_fields_are_passed_to_ModifyDocument_handler()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var event1 = new Events<string>.FirstEvent(docId, _fixture.Create<string>());
        var event2 = new Events<string>.FirstEvent(docId, _fixture.Create<string>());
        var events = ImmutableList.Create<object>(event1, event2);

        var projection = new ComplexIdContextVerifyingProjection(
            events,
            idContext);

        var storageSetup = new SetupInMemoryStorage();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        // ModifyDocument handler captures the Id from the projection context
        projection.ReceivedContextsViaModifyDocument.ShouldNotBeEmpty();
        foreach (var receivedId in projection.ReceivedContextsViaModifyDocument)
        {
            receivedId.DocumentId.ShouldBe(docId);
            receivedId.Child.ShouldNotBeNull();
            receivedId.Child.Data.ShouldBe(childData);
        }
    }

    [Fact]
    public async Task Complex_id_context_fields_are_passed_to_all_chained_handlers()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var events = Enumerable.Range(0, 5)
            .Select(_ => (object)new Events<string>.FirstEvent(docId, _fixture.Create<string>()))
            .ToImmutableList();

        var projection = new ComplexIdContextVerifyingProjection(events, idContext);
        var storageSetup = new SetupInMemoryStorage();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        // Both HandleWith and ModifyDocument handlers should each have been called once per event
        projection.ReceivedContexts.Count.ShouldBe(events.Count);
        projection.ReceivedContextsViaModifyDocument.Count.ShouldBe(events.Count);

        foreach (var ctx in projection.ReceivedContexts.Concat(projection.ReceivedContextsViaModifyDocument))
        {
            ctx.DocumentId.ShouldBe(docId);
            ctx.Child.Data.ShouldBe(childData);
        }
    }

    /// <summary>
    /// A projection that uses two chained HandleWith calls for the same event type,
    /// capturing the full ComplexIdContext in each handler so we can assert its fields.
    /// Both handlers are registered under a single On&lt;&gt; chain because registering a
    /// second On&lt;TEvent&gt; for the same type replaces the first.
    /// </summary>
    private class ComplexIdContextVerifyingProjection(
        IEnumerable<object> events,
        ProjectionWithComplexIdContextTests.ComplexIdContext idContext)
        : InMemoryProjection<ProjectionWithComplexIdContextTests.ComplexIdContext, TestDocument<string>>
    {
        public override string Name => "ComplexIdContextVerifyingProjection";
        public override TimeSpan ProjectionTimeout => TimeSpan.FromSeconds(5);

        public ConcurrentBag<ProjectionWithComplexIdContextTests.ComplexIdContext> ReceivedContexts { get; } = new();
        public ConcurrentBag<ProjectionWithComplexIdContextTests.ComplexIdContext> ReceivedContextsViaModifyDocument { get; } = new();

        private readonly IAsyncEnumerable<EventWithPosition> _events =
            events.Select((x, i) => new EventWithPosition(x, i + 1)).ToAsyncEnumerable();

        public override ISetupProjectionHandlers<
                ProjectionWithComplexIdContextTests.ComplexIdContext,
                InMemoryProjectionContext<ProjectionWithComplexIdContextTests.ComplexIdContext, TestDocument<string>>>
            Configure(ISetupProjection<
                ProjectionWithComplexIdContextTests.ComplexIdContext,
                InMemoryProjectionContext<ProjectionWithComplexIdContextTests.ComplexIdContext, TestDocument<string>>> config)
        {
            return config
                .On<Events<string>.FirstEvent>(_ => idContext)
                // First HandleWith: captures the id context and initialises/updates the document
                .HandleWith((evnt, ctx, _, _) =>
                {
                    ReceivedContexts.Add(ctx.Id);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    });
                    return Task.CompletedTask;
                })
                // Second HandleWith on the same On<>: captures via DocumentHandlingMetaData
                .HandleWith((_, ctx, position, _) =>
                {
                    ReceivedContextsViaModifyDocument.Add(ctx.Id);
                    return Task.CompletedTask;
                });
        }

        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(() => _events)
                .Where(x => fromPosition == null || x.Position > fromPosition);
        }
    }
}

