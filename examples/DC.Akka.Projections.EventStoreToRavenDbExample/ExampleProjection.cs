using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Persistence.EventStore.Query;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.EventStoreToRavenDbExample;

public class ExampleProjection(ActorSystem actorSystem) : StringIdProjection<ExampleDocument>
{
    public override ISetupProjection<string, ExampleDocument> Configure(
        ISetupProjection<string, ExampleDocument> config)
    {
        return config
            .TransformUsing<Events.ThirdEvent>(
                evnt => ImmutableList.Create<object>(
                    new Events.FirstEvent(evnt.Slug, evnt.StringEventId, evnt.StringTestData),
                    new Events.SecondEvent(evnt.Slug, evnt.IntEventId, evnt.IntTestData)))
            .On<Events.FirstEvent>(
                evnt => ExampleDocument.BuildId(evnt.Slug),
                (evnt, doc) =>
                {
                    doc ??= new ExampleDocument
                    {
                        Id = ExampleDocument.BuildId(evnt.Slug),
                        Slug = evnt.Slug
                    };

                    doc.ProjectedEvents = doc.ProjectedEvents.SetItem(evnt.EventId, evnt);

                    return doc;
                })
            .On<Events.SecondEvent>(
                evnt => ExampleDocument.BuildId(evnt.Slug),
                (evnt, doc) =>
                {
                    doc ??= new ExampleDocument
                    {
                        Id = ExampleDocument.BuildId(evnt.Slug),
                        Slug = evnt.Slug
                    };

                    doc.ProjectedEvents = doc.ProjectedEvents.SetItem(evnt.EventId, evnt);

                    return doc;
                });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return PersistenceQuery.Get(actorSystem)
            .ReadJournalFor<EventStoreReadJournal>(
                actorSystem.Settings.Config.GetString("akka.persistence.query.plugin"))
            .CurrentAllEvents(fromPosition.HasValue ? Offset.Sequence(fromPosition.Value) : Offset.NoOffset())
            .Select(evnt => new EventWithPosition(
                evnt.Event,
                evnt.Offset is Sequence seq ? seq.Value : null));
    }
}