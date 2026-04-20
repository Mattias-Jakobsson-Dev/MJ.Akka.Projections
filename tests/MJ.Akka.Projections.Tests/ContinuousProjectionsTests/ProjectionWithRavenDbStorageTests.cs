using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using AutoFixture;
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
        return new Events<string>.EventWithDataTransform(documentId, Fixture.Create<string>(), data, transformTo.OfType<Events<string>.IEvent>().ToImmutableList());
    }

    protected override Task VerifyDataContext(
        SimpleIdContext<string> documentId,
        RavenDbProjectionContext<TestDocument<string>> context,
        string expectedData)
    {
        context.Document!.ReceivedData.ShouldContain(expectedData);
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
                .WhenAny(h => h.ModifyDocument((evnt, doc) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                }))
                .On<Events<string>.EventWithFilter>().WithId(x => x.DocId)
                .When(filter => filter.WithEventFilter(evnt => evnt.Filter()), h => h.ModifyDocument((evnt, doc) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                }))
                .On<Events<string>.DelayHandlingWithoutCancellationToken>().WithId(x => x.DocId)
                .WhenAny(h => h.ModifyDocument((evnt, doc) =>
                {
                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                }))
                .On<Events<string>.DelayHandlingWithCancellationToken>().WithId(x => x.DocId)
                .WhenAny(h => h.ModifyDocument(async (evnt, doc, cancellationToken) =>
                {
                    await Task.Delay(evnt.Delay, cancellationToken);
                    
                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                }))
                .On<Events<string>.FailProjection>().WithId(x => x.DocId)
                .WhenAny(h => h.ModifyDocument((evnt, doc) =>
                {
                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

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

                    doc.AddHandledEvent(evnt.EventId);

                    doc.PreviousEventFailures = doc.PreviousEventFailures.SetItem(
                        evnt.EventId,
                        documentFailures[evnt.FailureKey]);

                    return doc;
                }))
                .On<Events<string>.EventThatDoesntGetDocumentId>().WithId(_ => null)
                .WhenAny(h => h.ModifyDocument((evnt, doc) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                }))
                // WithData: data drives the document id
                .On<Events<string>.EventWithDataId>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .WithId((evnt, data) =>
                {
                    if (data != evnt.Data) throw new Exception($"Data mismatch in GetId: expected '{evnt.Data}', got '{data}'");
                    return new SimpleIdContext<string>(evnt.DocId);
                })
                .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
                {
                    if (data != evnt.Data) throw new Exception($"Data mismatch in HandleWith: expected '{evnt.Data}', got '{data}'");
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        doc.ReceivedData = doc.ReceivedData.Add(data);
                        return doc;
                    });
                    return Task.CompletedTask;
                }))
                // WithData: data is forwarded to the handler
                .On<Events<string>.EventWithDataHandler>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .WithId((evnt, _) => new SimpleIdContext<string>(evnt.DocId))
                .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
                {
                    if (data != evnt.Data) throw new Exception($"Data mismatch in HandleWith: expected '{evnt.Data}', got '{data}'");
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        doc.ReceivedData = doc.ReceivedData.Add(data);
                        return doc;
                    });
                    return Task.CompletedTask;
                }))
                // WithData: data is used in transform
                .On<Events<string>.EventWithDataTransform>()
                .WithData(evnt => Task.FromResult(evnt.Data))
                .Transform((evnt, data) =>
                {
                    if (data != evnt.Data) throw new Exception($"Data mismatch in Transform: expected '{evnt.Data}', got '{data}'");
                    return evnt.TransformTo.OfType<object>().ToImmutableList();
                });
        }
        
        public override ILoadProjectionContext<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>> 
            GetLoadProjectionContext(SetupRavenDbStorage storageSetup)
        {
            return new LoaderWithStorageFailures<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument<string>>>(
                base.GetLoadProjectionContext(storageSetup),
                storageFailures);
        }
        
        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(events
                .Select((x, i) => new EventWithPosition(x, i + 1))
                .Where(x => fromPosition == null || x.Position > fromPosition)
                .ToImmutableList());
        }

        public override long? GetInitialPosition()
        {
            return initialPosition;
        }
    }
}