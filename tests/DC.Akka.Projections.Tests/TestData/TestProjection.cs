using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Tests.TestData;

public class TestProjection(IImmutableList<object> events) : IProjection<string, TestDocument>
{
    public string Name => "MutableTestProjection";

    public ISetupProjection<string, TestDocument> Configure(ISetupProjection<string, TestDocument> config)
    {
        return config
            .RegisterHandler<Events.FirstEvent>(
                x => x.DocId,
                (evnt, doc) =>
                {
                    doc ??= new TestDocument
                    {
                        Id = evnt.DocId
                    };

                    doc.HandledEvents = doc.HandledEvents.Add(evnt);

                    return doc;
                })
            .RegisterHandler<Events.SecondEvent>(
                x => x.DocId,
                (evnt, doc) =>
                {
                    doc ??= new TestDocument
                    {
                        Id = evnt.DocId
                    };

                    doc.HandledEvents = doc.HandledEvents.Add(evnt);

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