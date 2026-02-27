using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.RavenDb;

namespace MJ.Akka.Projections.Benchmarks;

public class RavenDbTestProjection : RavenDbProjection<RavenDbTestProjection.TestDocument>
{
    private readonly IImmutableList<TestEvent> _events;

    public RavenDbTestProjection(int numberOfEvents, int numberOfDocuments)
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
    
    public override ISetupProjectionHandlers<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>> Configure(
        ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>> config)
    {
        return config
            .On<TestEvent>(evnt => evnt.DocId)
            .ModifyDocument((evnt, doc) =>
            {
                if (doc == null)
                    return new TestDocument(evnt.DocId, 1);

                return doc with
                {
                    Version = doc.Version + 1
                };
            });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(_events
            .Select((evnt, index) => new EventWithPosition(evnt, index + 1)));
    }
    
    [PublicAPI]
    public record TestDocument(string DocId, int Version);

    public record TestEvent(string DocId);
}