using System.Collections.Immutable;

namespace DC.Akka.Projections.Tests.TestData;

public class TestDocument
{
    public string Id { get; set; } = null!;
    public IImmutableList<Events.IEvent> HandledEvents { get; set; } = ImmutableList<Events.IEvent>.Empty;
}