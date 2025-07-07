using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage.Batched;

public class BufferWithinStorageBatchingStrategy(int items, TimeSpan timeout) : IStorageBatchingStrategy
{
    public Source<IImmutableList<PendingWrite>, ISourceQueueWithComplete<PendingWrite>> 
        GetStrategy(Source<PendingWrite, ISourceQueueWithComplete<PendingWrite>> source)
    {
        return source
            .GroupedWithin(items, timeout)
            .Select(x =>
            {
                return x.Aggregate(
                    (IImmutableList<PendingWrite>)ImmutableList<PendingWrite>.Empty,
                    (current, pending) => current.Add(pending));
            });
    }
}