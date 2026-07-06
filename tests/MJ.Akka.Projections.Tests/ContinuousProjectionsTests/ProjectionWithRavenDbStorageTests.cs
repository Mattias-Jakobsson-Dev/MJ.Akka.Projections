using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Streams.Dsl;
using AutoFixture;
using MJ.Akka.Projections.Documents;
using Shouldly;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using MJ.Akka.Projections.Tests.Storage;
using MJ.Akka.Projections.Tests.TestData;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithRavenDbStorageTests(RavenDbFixture fixture, NormalTestKitActorSystem actorSystemSetup)
    : BaseContinuousProjectionsTests<
            SimpleIdContext<string>,
            RavenDbProjectionContext<TestDocument<string>>,
            SetupRavenDbStorage>(actorSystemSetup), IClassFixture<RavenDbFixture>,
        IClassFixture<NormalTestKitActorSystem>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();

    protected override SetupRavenDbStorage CreateStorageSetup()
    {
        return new SetupRavenDbStorage(_documentStore, new BulkInsertOptions());
    }

    protected override IProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>, SetupRavenDbStorage>
        GetProjection(
            IImmutableList<object> events,
            IImmutableList<StorageFailures> storageFailures,
            long? initialPosition = null)
    {
        return new TestProjection(events, storageFailures, initialPosition);
    }

    protected override object GetEventThatFails(SimpleIdContext<string> id, int numberOfFailures)
    {
        return new Events<string>.FailProjection(
            id,
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            numberOfFailures,
            new Exception("Projection failed"));
    }

    protected override object GetTestEvent(SimpleIdContext<string> documentId)
    {
        return new Events<string>.FirstEvent(documentId, Fixture.Create<string>());
    }

    protected override object GetTransformationEvent(SimpleIdContext<string> documentId, IImmutableList<object> transformTo)
    {
        return new Events<string>.TransformToMultipleEvents(transformTo.OfType<Events<string>.IEvent>()
            .ToImmutableList());
    }

    protected override object GetUnMatchedEvent(SimpleIdContext<string> documentId)
    {
        return new Events<string>.UnHandledEvent(documentId);
    }

    protected override object GetEventThatIsFilteredOut(SimpleIdContext<string> documentId)
    {
        return new Events<string>.EventWithFilter(documentId, Fixture.Create<string>(), () => false);
    }

    protected override object GetEventThatDoesntGetDocumentId(SimpleIdContext<string> documentId)
    {
        return new Events<string>.EventThatDoesntGetDocumentId(documentId, Fixture.Create<string>());
    }

    protected override object GetEventWithDataForId(SimpleIdContext<string> documentId, string data)
    {
        return new Events<string>.EventWithDataId(documentId, Fixture.Create<string>(), data);
    }

    protected override object GetEventWithDataForHandler(SimpleIdContext<string> documentId, string data)
    {
        return new Events<string>.EventWithDataHandler(documentId, Fixture.Create<string>(), data);
    }

    protected override object GetEventWithDataForTransform(SimpleIdContext<string> documentId, string data, IImmutableList<object> transformTo)
    {
        return new Events<string>.EventWithDataTransform(
            documentId,
            Fixture.Create<string>(),
            data,
            transformTo.OfType<Events<string>.IEvent>().ToImmutableList());
    }

    protected override object GetEventWithTransformAndHandler(
        SimpleIdContext<string> documentId, string originalEventId, string transformedEventId)
    {
        return new Events<string>.TransformAndHandleEvent(documentId, originalEventId, transformedEventId);
    }

    protected override object GetEventWithDataTransformAndHandler(
        SimpleIdContext<string> documentId, string originalEventId, string transformedEventId, string data)
    {
        return new Events<string>.TransformAndHandleWithDataEvent(documentId, originalEventId, transformedEventId, data);
    }

    protected override Task VerifyDataContext(
        SimpleIdContext<string> documentId,
        RavenDbProjectionContext<TestDocument<string>> context,
        string expectedData)
    {
        context.Document!.ReceivedData.ShouldContain(expectedData);
        return Task.CompletedTask;
    }

    protected override Task VerifyTransformAndHandlerContext(
        SimpleIdContext<string> documentId,
        RavenDbProjectionContext<TestDocument<string>> context,
        string originalEventId,
        string transformedEventId)
    {
        context.Document!.HandledEvents.ShouldContain(originalEventId);
        context.Document!.HandledEvents.ShouldContain(transformedEventId);
        context.Document!.HandledEvents.Count.ShouldBe(2);
        return Task.CompletedTask;
    }

    protected override Task VerifyDataTransformAndHandlerContext(
        SimpleIdContext<string> documentId,
        RavenDbProjectionContext<TestDocument<string>> context,
        string originalEventId,
        string transformedEventId,
        string data)
    {
        context.Document!.HandledEvents.ShouldContain(originalEventId);
        context.Document!.HandledEvents.ShouldContain(transformedEventId);
        context.Document!.HandledEvents.Count.ShouldBe(2);
        context.Document!.ReceivedData.ShouldContain(data);
        return Task.CompletedTask;
    }

    protected override Task VerifyContext(
        SimpleIdContext<string> documentId,
        RavenDbProjectionContext<TestDocument<string>> context,
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
            .Where(x => x.DocId.ToString() == documentId)
            .ToImmutableList();

        context.Document!.HandledEvents.Count.ShouldBe(eventsToCheck.Count);

        var position = 1;

        foreach (var evnt in eventsToCheck)
        {
            context.Document!.HandledEvents.ShouldContain(evnt.EventId);
            context.Document!.EventHandledOrder[evnt.EventId].ShouldBe(position);

            position++;
        }

        var testProjection = (TestProjection)projection;

        testProjection.HandledEvents.Count.ShouldBe(projectedEvents.Count);

        return Task.CompletedTask;
    }

    private class TestProjection(
        IImmutableList<object> events,
        IImmutableList<StorageFailures> storageFailures,
        long? initialPosition) : RavenDbProjection<TestDocument<string>>
    {
        public ConcurrentDictionary<string, Events<string>.IEvent> HandledEvents { get; } = new();
        
        public override ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>> Configure(
            ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>> config)
        {
            var runFailures = new ConcurrentDictionary<string, Dictionary<string, int>>();

            return config
                .On<Events<string>.TransformToMultipleEvents>().Transform(evnt =>
                    evnt.Events.OfType<object>().ToImmutableList())
                .On<Events<string>.FirstEvent>().WithId(x => x.DocId)
                .WhenAny(h => h.HandleWith((evnt, _, _, _) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    return Task.CompletedTask;
                }))
                .WhenDocumentNotExists(h => h.CreateDocument(evnt => new TestDocument<string> { Id = evnt.DocId })
                    .ModifyDocument((evnt, doc) =>
                    {
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    }))
                .WhenDocumentExists(h => h.ModifyDocument((evnt, doc) =>
                {
                    doc.AddHandledEvent(evnt.EventId);
                    return doc;
                }))
                .On<Events<string>.EventWithFilter>().WithId(x => x.DocId)
                .When(filter => filter.WithEventFilter(evnt => evnt.Filter()), h => h.HandleWith((evnt, _, _, _) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    return Task.CompletedTask;
                }))
                .WhenDocumentNotExists(
                    h => h.CreateDocument(evnt => new TestDocument<string> { Id = evnt.DocId })
                          .ModifyDocument((evnt, doc) =>
                          {
                              doc.AddHandledEvent(evnt.EventId);
                              return doc;
                          }),
                    filter => filter.WithEventFilter(evnt => evnt.Filter()))
                .WhenDocumentExists(
                    h => h.ModifyDocument((evnt, doc) =>
                    {
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    }),
                    filter => filter.WithEventFilter(evnt => evnt.Filter()))
                .On<Events<string>.DelayHandlingWithoutCancellationToken>().WithId(x => x.DocId)
                .WhenDocumentNotExists(h => h.CreateDocument(evnt => new TestDocument<string> { Id = evnt.DocId }))
                .WhenDocumentExists(h => h.ModifyDocument((evnt, doc) =>
                {
                    doc.AddHandledEvent(evnt.EventId);
                    return doc;
                }))
                .On<Events<string>.DelayHandlingWithCancellationToken>().WithId(x => x.DocId)
                .WhenDocumentNotExists(h => h.CreateDocument(evnt => new TestDocument<string> { Id = evnt.DocId }))
                .WhenDocumentExists(h => h.ModifyDocument(async (evnt, doc, cancellationToken) =>
                {
                    await Task.Delay((int)evnt.Delay.TotalMilliseconds, cancellationToken);
                    doc.AddHandledEvent(evnt.EventId);
                    return doc;
                }))
                .On<Events<string>.FailProjection>().WithId(x => x.DocId)
                .WhenAny(h => h.HandleWith((evnt, _, _, _) =>
                {
                    var documentFailures = runFailures.GetOrAdd(
                        evnt.DocId,
                        _ => new Dictionary<string, int>());

                    documentFailures.TryAdd(evnt.FailureKey, 0);

                    if (documentFailures[evnt.FailureKey] < evnt.ConsecutiveFailures)
                    {
                        documentFailures[evnt.FailureKey]++;
                        throw evnt.FailWith;
                    }

                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    return Task.CompletedTask;
                }))
                .WhenDocumentNotExists(h => h.CreateDocument(evnt => new TestDocument<string> { Id = evnt.DocId })
                    .ModifyDocument((evnt, doc) =>
                    {
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    }))
                .WhenDocumentExists(h => h.ModifyDocument((evnt, doc) =>
                {
                    var documentFailures = runFailures.GetOrAdd(
                        evnt.DocId,
                        _ => new Dictionary<string, int>());

                    doc.AddHandledEvent(evnt.EventId);
                    doc.PreviousEventFailures = doc.PreviousEventFailures.SetItem(
                        evnt.EventId,
                        documentFailures.GetValueOrDefault(evnt.FailureKey, 0));
                    return doc;
                }))
                .On<Events<string>.EventThatDoesntGetDocumentId>().WithId(_ => null)
                .WhenAny(h => h.HandleWith((evnt, _, _, _) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    return Task.CompletedTask;
                }))
                .WhenDocumentNotExists(h => h.CreateDocument(evnt => new TestDocument<string> { Id = evnt.DocId })
                    .ModifyDocument((evnt, doc) =>
                    {
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    }))
                .WhenDocumentExists(h => h.ModifyDocument((evnt, doc) =>
                {
                    doc.AddHandledEvent(evnt.EventId);
                    return doc;
                }))
                .On<Events<string>.EventWithDataId>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .WithId((evnt, _) => evnt.DocId)
                .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
                {
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.ReceivedData = doc.ReceivedData.Add(data);
                        return doc;
                    });
                    return Task.CompletedTask;
                }))
                .On<Events<string>.EventWithDataHandler>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .WithId((evnt, _) => evnt.DocId)
                .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
                {
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.ReceivedData = doc.ReceivedData.Add(data);
                        return doc;
                    });
                    return Task.CompletedTask;
                }))
                .On<Events<string>.EventWithDataTransform>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .Transform((evnt, _) => evnt.TransformTo.OfType<object>().ToImmutableList())
                // TransformAndHandleEvent: transforms to SecondaryEvent AND handles the original event
                .On<Events<string>.TransformAndHandleEvent>()
                .Transform(evnt =>
                    ImmutableList.Create<object>(new Events<string>.SecondaryEvent(evnt.DocId, evnt.TransformedEventId)))
                .WithId(x => x.DocId)
                .WhenAny(h => h.HandleWith((evnt, ctx, _, _) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    });
                    return Task.CompletedTask;
                }))
                // SecondaryEvent: produced by TransformAndHandleEvent and TransformAndHandleWithDataEvent
                .On<Events<string>.SecondaryEvent>().WithId(x => x.DocId)
                .WhenAny(h => h.HandleWith((evnt, ctx, _, _) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    });
                    return Task.CompletedTask;
                }))
                // TransformAndHandleWithDataEvent: transforms to SecondaryEvent AND handles the original event with data
                .On<Events<string>.TransformAndHandleWithDataEvent>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .Transform((evnt, _) =>
                    ImmutableList.Create<object>(new Events<string>.SecondaryEvent(evnt.DocId, evnt.TransformedEventId)))
                .WithId((evnt, _) => evnt.DocId)
                .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        doc.ReceivedData = doc.ReceivedData.Add(data);
                        return doc;
                    });
                    return Task.CompletedTask;
                }));
        }
        
        public override ILoadProjectionContext<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>> 
            GetLoadProjectionContext(SetupRavenDbStorage storageSetup)
        {
            return new LoaderWithStorageFailures<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>>(
                base.GetLoadProjectionContext(storageSetup),
                storageFailures);
        }
        
        public override Task<IProjectionEventSource> GetSource() =>
            Task.FromResult<IProjectionEventSource>(new SimpleProjectionEventSource((fromPosition, _) => Source.From(events
                .Select((x, i) => new EventWithPosition(x, i + 1))
                .Where(x => fromPosition == null || x.Position > fromPosition)
                .ToImmutableList())));

        public override long? GetInitialPosition()
        {
            return initialPosition;
        }
    }
}