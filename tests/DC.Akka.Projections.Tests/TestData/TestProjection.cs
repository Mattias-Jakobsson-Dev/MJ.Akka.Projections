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
    : IProjection<TId, TestDocument<TId>> 
    where TId : notnull
{
    public static string GetName()
    {
        return nameof(TestProjection<TId>);
    }
    
    public string Name => GetName();
    
    public TId IdFromString(string id)
    {
        return (TId)TestProjection.IdFromStringParsers[typeof(TId)](id);
    }

    public string IdToString(TId id)
    {
        return id.ToString() ?? "";
    }

    public ISetupProjection<TId, TestDocument<TId>> Configure(ISetupProjection<TId, TestDocument<TId>> config)
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

    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(events
            .Select((x, i) => new EventWithPosition(x, i + 1))
            .Where(x => fromPosition == null || x.Position >= fromPosition)
            .ToImmutableList());
    }
}