using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.TestData;

[PublicAPI]
public class TestDocument<TId>
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
}