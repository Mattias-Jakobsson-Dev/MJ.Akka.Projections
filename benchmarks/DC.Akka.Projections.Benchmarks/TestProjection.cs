using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Benchmarks;

public class TestProjection : IProjection<string, TestProjection.TestDocument>
{
    private readonly IImmutableList<TestEvent> _events;

    public TestProjection(int numberOfEvents, int numberOfDocuments)
    {
        var currentDocumentId = 1;

        var events = new List<TestEvent>();
        
        for (var i = 0; i < numberOfEvents; i++)
        {
            events.Add(new TestEvent(currentDocumentId.ToString()));

            currentDocumentId++;

            if (currentDocumentId > numberOfDocuments)
                currentDocumentId = 1;
        }

        _events = events.ToImmutableList();
    }

    public string Name => nameof(TestProjection);
    
    public ISetupProjection<string, TestDocument> Configure(ISetupProjection<string, TestDocument> config)
    {
        return config
            .On<TestEvent>(
                evnt => evnt.DocId,
                (evnt, doc) =>
                {
                    if (doc == null)
                        return new TestDocument(evnt.DocId, 1);

                    return doc with
                    {
                        Version = doc.Version + 1
                    };
                });
    }

    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(_events
            .Select((evnt, index) => new EventWithPosition(evnt, index + 1)));
    }

    [PublicAPI]
    public record TestDocument(string DocId, int Version);
    
    private record TestEvent(string DocId);
}