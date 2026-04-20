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
    IAsyncEnumerable<EventWithPosition> events,
    IImmutableList<StorageFailures> failures,
    string? overrideName = null,
    long? initialPosition = null)
    : InMemoryProjection<TId, TestDocument<TId>>
    where TId : notnull
{
    public TestProjection(
        IEnumerable<object> events,
        IImmutableList<StorageFailures> failures,
        string? overrideName = null,
        long? initialPosition = null) : this(
        events.Select((x, i) => new EventWithPosition(x, i + 1)).ToAsyncEnumerable(),
        failures,
        overrideName,
        initialPosition)
    {
    }

    public override TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(5);

    public ConcurrentDictionary<string, Events<TId>.IEvent> HandledEvents { get; } = new();

    private static string GetName() => $"TestProjectionOf{typeof(TId).Name}";

    public override string Name => !string.IsNullOrEmpty(overrideName) ? overrideName : GetName();

    public override long? GetInitialPosition() => initialPosition;

    public override ILoadProjectionContext<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TestDocument<TId>>>
        GetLoadProjectionContext(SetupInMemoryStorage storageSetup)
    {
        return new LoaderWithStorageFailures<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TestDocument<TId>>>(
            base.GetLoadProjectionContext(storageSetup),
            failures);
    }

    public override ISetupProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TestDocument<TId>>> Configure(
        ISetupProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TestDocument<TId>>> config)
    {
        var runFailures = new ConcurrentDictionary<TId, Dictionary<string, int>>();

        return config
            .On<Events<TId>.TransformToMultipleEvents>().Transform(evnt =>
                evnt.Events.OfType<object>().ToImmutableList())
            .On<Events<TId>.FirstEvent>().WithId(x => x.DocId)
            .WhenAny(h => h.ModifyDocument((evnt, doc) =>
            {
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                doc ??= new TestDocument<TId> { Id = evnt.DocId };
                doc.AddHandledEvent(evnt.EventId);
                return doc;
            }))
            .On<Events<TId>.EventWithFilter>().WithId(x => x.DocId)
            .When(filter => filter.WithEventFilter(evnt => evnt.Filter()), h => h.ModifyDocument((evnt, doc) =>
            {
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                doc ??= new TestDocument<TId> { Id = evnt.DocId };
                doc.AddHandledEvent(evnt.EventId);
                return doc;
            }))
            .On<Events<TId>.DelayHandlingWithoutCancellationToken>().WithId(x => x.DocId)
            .WhenAny(h => h.ModifyDocument(async (evnt, doc) =>
            {
                await Task.Delay(evnt.Delay);
                doc ??= new TestDocument<TId> { Id = evnt.DocId };
                doc.AddHandledEvent(evnt.EventId);
                return doc;
            }))
            .On<Events<TId>.DelayHandlingWithCancellationToken>().WithId(x => x.DocId)
            .WhenAny(h => h.ModifyDocument(async (evnt, doc, cancellationToken) =>
            {
                await Task.Delay(evnt.Delay, cancellationToken);
                doc ??= new TestDocument<TId> { Id = evnt.DocId };
                doc.AddHandledEvent(evnt.EventId);
                return doc;
            }))
            .On<Events<TId>.FailProjection>().WithId(x => x.DocId)
            .WhenAny(h => h.ModifyDocument((evnt, doc) =>
            {
                doc ??= new TestDocument<TId> { Id = evnt.DocId };
                var documentFailures = runFailures.GetOrAdd(evnt.DocId, _ => new Dictionary<string, int>());
                documentFailures.TryAdd(evnt.FailureKey, 0);
                if (documentFailures[evnt.FailureKey] < evnt.ConsecutiveFailures)
                {
                    documentFailures[evnt.FailureKey]++;
                    throw evnt.FailWith;
                }
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                doc.AddHandledEvent(evnt.EventId);
                doc.PreviousEventFailures = doc.PreviousEventFailures.SetItem(
                    evnt.EventId, documentFailures[evnt.FailureKey]);
                return doc;
            }))
            .On<Events<TId>.EventThatDoesntGetDocumentId>().WithId(_ => null)
            .WhenAny(h => h.ModifyDocument((evnt, doc) =>
            {
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                doc ??= new TestDocument<TId> { Id = evnt.DocId };
                doc.AddHandledEvent(evnt.EventId);
                return doc;
            }))
            // WithData: data drives the document id
            .On<Events<TId>.EventWithDataId>()
            .WithData(evnt => Task.FromResult(evnt.Data))
            .WithId((evnt, data) =>
            {
                // Verify data matches what the event carries
                if (data != evnt.Data) throw new Exception($"Data mismatch in GetId: expected '{evnt.Data}', got '{data}'");
                return new SimpleIdContext<TId>(evnt.DocId);
            })
            .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
            {
                if (data != evnt.Data) throw new Exception($"Data mismatch in HandleWith: expected '{evnt.Data}', got '{data}'");
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                ctx.ModifyDocument(doc =>
                {
                    doc ??= new TestDocument<TId> { Id = evnt.DocId };
                    doc.AddHandledEvent(evnt.EventId);
                    doc.ReceivedData = doc.ReceivedData.Add(data);
                    return doc;
                });
                return Task.CompletedTask;
            }))
            // WithData: data is forwarded to the handler
            .On<Events<TId>.EventWithDataHandler>()
            .WithData(evnt => Task.FromResult(evnt.Data))
            .WithId((evnt, _) => new SimpleIdContext<TId>(evnt.DocId))
            .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
            {
                if (data != evnt.Data) throw new Exception($"Data mismatch in HandleWith: expected '{evnt.Data}', got '{data}'");
                HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);
                ctx.ModifyDocument(doc =>
                {
                    doc ??= new TestDocument<TId> { Id = evnt.DocId };
                    doc.AddHandledEvent(evnt.EventId);
                    doc.ReceivedData = doc.ReceivedData.Add(data);
                    return doc;
                });
                return Task.CompletedTask;
            }))
            // WithData: data is used in transform
            .On<Events<TId>.EventWithDataTransform>()
            .WithData(evnt => Task.FromResult(evnt.Data))
            .Transform((evnt, data) =>
            {
                if (data != evnt.Data) throw new Exception($"Data mismatch in Transform: expected '{evnt.Data}', got '{data}'");
                return evnt.TransformTo.OfType<object>().ToImmutableList();
            });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(() => events)
            .SelectAsync(1, async evnt =>
            {
                if (fromPosition.HasValue && evnt.Position <= fromPosition && evnt is IEventWithAck eventWithAck)
                    await eventWithAck.Ack(CancellationToken.None);
                return evnt;
            })
            .Where(x => fromPosition == null || x.Position > fromPosition);
    }
}