using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage;

public class BufferWithinStorageBatchingStrategy(int items, TimeSpan timeout) : IStorageBatchingStrategy
{
    public Source<IImmutableList<IPendingWrite>, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source)
    {
        return source
            .GroupedWithin(items, timeout)
            .Select(x =>
            {
                return x.Aggregate(
                    (IImmutableList<IPendingWrite>)ImmutableList<IPendingWrite>.Empty,
                    (current, pending) => current.Add(pending));
            });
    }
}