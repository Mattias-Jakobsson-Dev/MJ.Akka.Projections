using System.Collections.Immutable;
using MJ.Akka.Projections;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.TestData;

[PublicAPI]
public class TestDocument<TId> : IResetDocument<TestDocument<TId>>
{
    public TId Id { get; set; } = default!;
    public IImmutableList<string> HandledEvents { get; set; } = ImmutableList<string>.Empty;
    public IImmutableDictionary<string, int> EventHandledOrder { get; set; } = ImmutableDictionary<string, int>.Empty;
    public IImmutableDictionary<string, int> PreviousEventFailures { get; set; } = ImmutableDictionary<string, int>.Empty;

    public void AddHandledEvent(string eventId)
    {
        if (!HandledEvents.Contains(eventId))
            HandledEvents = HandledEvents.Add(eventId);

        if (!EventHandledOrder.ContainsKey(eventId))
            EventHandledOrder = EventHandledOrder.SetItem(eventId, EventHandledOrder.Count + 1);
    }
    
    public TestDocument<TId> Reset()
    {
        return new TestDocument<TId>
        {
            Id = Id,
            HandledEvents = HandledEvents,
            EventHandledOrder = EventHandledOrder,
            PreviousEventFailures = PreviousEventFailures
        };
    }
}