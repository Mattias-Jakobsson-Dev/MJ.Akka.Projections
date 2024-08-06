using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Tests.TestData;

public class TestProjection<TId>(IImmutableList<object> events) : IProjection<TId, TestDocument<TId>> 
    where TId : notnull
{
    public string Name => nameof(TestProjection<TId>);

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