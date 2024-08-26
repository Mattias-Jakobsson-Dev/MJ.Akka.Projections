using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public class BatchWithinEventBatchingStrategy(int maxItems, TimeSpan timeout, int parallelism) : IEventBatchingStrategy
{
    public int GetParallelism()
    {
        return parallelism;
    }

    public Source<ImmutableList<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source)
    {
        return source
            .GroupedWithin(maxItems, timeout)
            .Select(x => x.ToImmutableList());
    }
}