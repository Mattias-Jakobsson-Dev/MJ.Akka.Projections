using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.TestData;

public class TestProjection<TId>(
    IImmutableList<object> events,
    IImmutableList<StorageFailures> failures,
    string? overrideName = null,
    long? initialPosition = null)
    : InMemoryProjection<SimpleIdContext<TId>, TestDocument<TId>>
    where TId : notnull
{
    public override TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(5);

    public ConcurrentDictionary<string, Events<TId>.IEvent> HandledEvents { get; } = new();

    private static string GetName()
    {
        return $"TestProjectionOf{typeof(TId).Name}";
    }

    public override string Name => !string.IsNullOrEmpty(overrideName) ? overrideName : GetName();

    public override long? GetInitialPosition()
    {
        return initialPosition;
    }
    
    public override ILoadProjectionContext<SimpleIdContext<TId>, InMemoryProjectionContext<SimpleIdContext<TId>, TestDocument<TId>>> 
        GetLoadProjectionContext(SetupInMemoryStorage storageSetup)
    {
        return new LoaderWithStorageFailures<SimpleIdContext<TId>, InMemoryProjectionContext<SimpleIdContext<TId>, TestDocument<TId>>>(
            base.GetLoadProjectionContext(storageSetup),
            failures);
    }

    public override ISetupProjectionHandlers<SimpleIdContext<TId>, InMemoryProjectionContext<SimpleIdContext<TId>, TestDocument<TId>>> Configure(
        ISetupProjection<SimpleIdContext<TId>, InMemoryProjectionContext<SimpleIdContext<TId>, TestDocument<TId>>> config)
    {
        var runFailures = new ConcurrentDictionary<TId, Dictionary<string, int>>();

        return config
            .TransformUsing<Events<TId>.TransformToMultipleEvents>(evnt =>
                evnt.Events.OfType<object>().ToImmutableList())
            .On<Events<TId>.FirstEvent>(x => x.DocId)
            .ModifyDocument((evnt, doc) =>
            {
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                doc ??= new TestDocument<TId>
                {
                    Id = evnt.DocId
                };

                doc.AddHandledEvent(evnt.EventId);

                return doc;
            })
            .On<Events<TId>.EventWithFilter>(
                x => x.DocId,
                filter => filter.WithEventFilter(evnt => evnt.Filter()))
            .ModifyDocument((evnt, doc) =>
            {
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                doc ??= new TestDocument<TId>
                {
                    Id = evnt.DocId
                };

                doc.AddHandledEvent(evnt.EventId);

                return doc;
            })
            .On<Events<TId>.DelayHandlingWithoutCancellationToken>(x => x.DocId)
            .ModifyDocument(async (evnt, doc) =>
            {
                await Task.Delay(evnt.Delay);
                
                doc ??= new TestDocument<TId>
                {
                    Id = evnt.DocId
                };

                doc.AddHandledEvent(evnt.EventId);

                return doc;
            })
            .On<Events<TId>.DelayHandlingWithCancellationToken>(x => x.DocId)
            .ModifyDocument(async (evnt, doc, cancellationToken) =>
            {
                await Task.Delay(evnt.Delay, cancellationToken);
                
                doc ??= new TestDocument<TId>
                {
                    Id = evnt.DocId
                };

                doc.AddHandledEvent(evnt.EventId);

                return doc;
            })
            .On<Events<TId>.FailProjection>(x => x.DocId)
            .ModifyDocument((evnt, doc) =>
            {
                doc ??= new TestDocument<TId>
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
            });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(events
            .Select((x, i) => new EventWithPosition(x, i + 1))
            .Where(x => fromPosition == null || x.Position > fromPosition)
            .ToImmutableList());
    }
}