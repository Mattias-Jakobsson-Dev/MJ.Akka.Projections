using System.Collections.Immutable;
using Akka.Persistence;
using Akka.Actor;

namespace MJ.Akka.Projections.EventStoreToRavenDbExample;

public class EventFeeder : ReceivePersistentActor
{
    private readonly Random _random = new();
    
    public static class Commands
    {
        public record FeedEvents(int NumberOfEvents);
    }
    
    public static class Responses
    {
        public record FeedEventsResponse;
    }

    private readonly string _id;

    public EventFeeder(string id)
    {
        _id = id;

        var eventFactories = new List<Func<object>>
            {
                () => new Events.FirstEvent(_id, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                () => new Events.SecondEvent(_id, Guid.NewGuid().ToString(), _random.Next(10000)),
                () => new Events.ThirdEvent(_id, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(), _random.Next(1000))
            }
            .ToImmutableList();

        Command<Commands.FeedEvents>(cmd =>
        {
            var events = Enumerable
                .Range(0, cmd.NumberOfEvents)
                .Select(_ => eventFactories[_random.Next(0, eventFactories.Count - 1)]())
                .ToImmutableList();

            PersistAll(events, _ => { });

            DeferAsync("saved", _ => Sender.Tell(new Responses.FeedEventsResponse()));
        });
    }
    
    public override string PersistenceId => $"event-feeders-{_id}";
}