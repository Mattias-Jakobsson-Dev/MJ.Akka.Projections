using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Tests.TestData;

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

public class TestProjection<TId>(IImmutableList<object> events) 
    : BaseProjection<TId, TestDocument<TId>> 
    where TId : notnull
{
    public static string GetName()
    {
        return $"TestProjectionOf{typeof(TId).Name}";
    }

    public override string Name => GetName();

    public override TId IdFromString(string id)
    {
        return (TId)TestProjection.IdFromStringParsers[typeof(TId)](id);
    }

    public override string IdToString(TId id)
    {
        return id.ToString() ?? "";
    }

    public override ISetupProjection<TId, TestDocument<TId>> Configure(ISetupProjection<TId, TestDocument<TId>> config)
    {
        return config
            .TransformUsing<Events<TId>.TransformToMultipleEvents>(
                evnt => evnt.Events.OfType<object>().ToImmutableList())
            .On<Events<TId>.FirstEvent>(
                x => x.DocId,
                (evnt, doc) =>
                {
                    doc ??= new TestDocument<TId>
                    {
                        Id = evnt.DocId
                    };

                    doc.HandledEvents = doc.HandledEvents.Add(evnt.EventId);

                    return doc;
                })
            .On<Events<TId>.SecondEvent>(
                x => x.DocId,
                (evnt, doc) =>
                {
                    doc ??= new TestDocument<TId>
                    {
                        Id = evnt.DocId
                    };

                    doc.HandledEvents = doc.HandledEvents.Add(evnt.EventId);

                    return doc;
                });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(events
            .Select((x, i) => new EventWithPosition(x, i + 1))
            .Where(x => fromPosition == null || x.Position >= fromPosition)
            .ToImmutableList());
    }
}