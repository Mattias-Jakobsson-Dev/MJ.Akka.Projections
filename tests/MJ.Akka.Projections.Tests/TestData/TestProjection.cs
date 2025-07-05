using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.TestData;

public static class TestProjection
{
    public static readonly IImmutableDictionary<Type, Func<string, object>> IdFromStringParsers =
        new Dictionary<Type, Func<string, object>>
            {
                [typeof(string)] = id => id.ToString(),
                [typeof(int)] = id => int.Parse(id)
            }
            .ToImmutableDictionary();
}

public class TestProjection<TId>(
    IImmutableList<object> events,
    IImmutableList<StorageFailures> failures,
    string? overrideName = null)
    : InMemoryProjection<TId, TestDocument<TId>>
    where TId : notnull
{
    public override TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(5);

    public ConcurrentDictionary<string, Events<TId>.IEvent> HandledEvents { get; } = new();

    private static string GetName()
    {
        return $"TestProjectionOf{typeof(TId).Name}";
    }

    public override string Name => !string.IsNullOrEmpty(overrideName) ? overrideName : GetName();

    public override TId IdFromString(string id)
    {
        return (TId)TestProjection.IdFromStringParsers[typeof(TId)](id);
    }

    public override string IdToString(TId id)
    {
        return id.ToString() ?? "";
    }

    public override ILoadProjectionContext<TId, InMemoryProjectionContext<TId, TestDocument<TId>>> 
        GetLoadProjectionContext(SetupInMemoryStorage storageSetup)
    {
        return new LoaderWithStorageFailures<TId, InMemoryProjectionContext<TId, TestDocument<TId>>>(
            base.GetLoadProjectionContext(storageSetup),
            failures);
    }

    public override ISetupProjection<TId, InMemoryProjectionContext<TId, TestDocument<TId>>> Configure(
        ISetupProjection<TId, InMemoryProjectionContext<TId, TestDocument<TId>>> config)
    {
        var runFailures = new ConcurrentDictionary<TId, Dictionary<string, int>>();

        return config
            .TransformUsing<Events<TId>.TransformToMultipleEvents>(evnt =>
                evnt.Events.OfType<object>().ToImmutableList())
            .On<Events<TId>.FirstEvent>(
                x => x.DocId,
                (evnt, context) =>
                {
                    context.ModifyDocument(doc =>
                    {
                        HandledEvents.AddOrUpdate(evnt.EventId, evnt, (_, _) => evnt);

                        doc ??= new TestDocument<TId>
                        {
                            Id = evnt.DocId
                        };

                        doc.AddHandledEvent(evnt.EventId);

                        return doc;
                    });
                })
            .On<Events<TId>.DelayHandlingWithoutCancellationToken>(
                x => x.DocId,
                async (evnt, context) =>
                {
                    await Task.Delay(evnt.Delay);

                    context.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<TId>
                        {
                            Id = evnt.DocId
                        };

                        doc.AddHandledEvent(evnt.EventId);

                        return doc;
                    });
                })
            .On<Events<TId>.DelayHandlingWithCancellationToken>(
                x => x.DocId,
                async (evnt, context, cancellationToken) =>
                {
                    await Task.Delay(evnt.Delay, cancellationToken);

                    context.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<TId>
                        {
                            Id = evnt.DocId
                        };

                        doc.AddHandledEvent(evnt.EventId);

                        return doc;
                    });
                })
            .On<Events<TId>.FailProjection>(
                x => x.DocId,
                (evnt, context) =>
                {
                    context.ModifyDocument(doc =>
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