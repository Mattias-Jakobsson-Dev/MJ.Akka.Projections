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
                .ModifyDocument((evnt, doc) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                })
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
                .ModifyDocument((evnt, doc) =>
                {
                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                })
                .On<Events<string>.DelayHandlingWithCancellationToken>().WithId(x => x.DocId)
                .ModifyDocument(async (evnt, doc, cancellationToken) =>
                {
                    await Task.Delay(evnt.Delay, cancellationToken);
                    
                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
                })
                .On<Events<string>.FailProjection>().WithId(x => x.DocId)
                .ModifyDocument((evnt, doc) =>
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
                })
                .On<Events<string>.EventThatDoesntGetDocumentId>().WithId(_ => null)
                .ModifyDocument((evnt, doc) =>
                {
                    HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                    doc ??= new TestDocument<string>
                    {
                        Id = evnt.DocId
                    };

                    doc.AddHandledEvent(evnt.EventId);

                    return doc;
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