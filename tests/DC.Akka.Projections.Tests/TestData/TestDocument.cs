using System.Collections.Immutable;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests.TestData;

[PublicAPI]
public class TestDocument<TId> : IResetDocument<TestDocument<TId>>
{
    public TId Id { get; set; } = default!;
    public IImmutableList<string> HandledEvents { get; set; } = ImmutableList<string>.Empty;
    
    public TestDocument<TId> Reset()
    {
        return new TestDocument<TId>
        {
            Id = Id,
            HandledEvents = HandledEvents
        };
    }
}