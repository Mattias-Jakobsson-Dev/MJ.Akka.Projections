using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Tests.TestData;

public class MutableTestProjection(IImmutableList<object> events) : IProjection<MutableTestDocument>
{
    public string Name => "MutableTestProjection";

    public ISetupProjection<MutableTestDocument> Configure(ISetupProjection<MutableTestDocument> config)
    {
        return config
            .RegisterHandler<Events.FirstEvent>(
                x => x.DocId,
                (evnt, doc) =>
                {
                    doc ??= new MutableTestDocument
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
                    doc ??= new MutableTestDocument
                    {
                        Id = evnt.DocId
                    };

                    doc.HandledEvents = doc.HandledEvents.Add(evnt);

                    return doc;
                });
    }

    public Source<IProjectionSourceData, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(events
            .Select((x, i) => new EventWithPosition(x, i + 1))
            .Where(x => fromPosition == null || x.Position >= fromPosition)
            .Select(x => new SourceData(x))
            .OfType<IProjectionSourceData>()
            .ToImmutableList());
    }
    
    private class SourceData(EventWithPosition evnt) : IProjectionSourceData
    {
        public IImmutableList<EventWithPosition> ParseEvents()
        {
            return ImmutableList.Create(evnt);
        }
    }
}