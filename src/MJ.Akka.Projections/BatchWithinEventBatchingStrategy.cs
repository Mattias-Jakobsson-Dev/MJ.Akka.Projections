using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public class BatchWithinEventBatchingStrategy(int maxItems, TimeSpan timeout) : IEventBatchingStrategy
{
    public static BatchWithinEventBatchingStrategy Default { get; } = new(100, TimeSpan.FromSeconds(1));
    
    public int GetParallelism()
    {
        return maxItems;
    }

    public Source<ImmutableList<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source)
    {
        return source
            .GroupedWithin(maxItems, timeout)
            .Select(x => x.ToImmutableList());
    }
}