using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.Batched;

[PublicAPI]
public class ThrottleWithinBatchingStrategy(int items, TimeSpan timeout) : IStorageBatchingStrategy
{
    public Source<IImmutableList<PendingWrite>, ISourceQueueWithComplete<PendingWrite>> GetStrategy(
        Source<PendingWrite, ISourceQueueWithComplete<PendingWrite>> source)
    {
        return source
            .Throttle(items, timeout, items, _ => 1, ThrottleMode.Shaping)
            .GroupedWithin(items, timeout)
            .Select(x =>
            {
                return x.Aggregate(
                    (IImmutableList<PendingWrite>)ImmutableList<PendingWrite>.Empty,
                    (current, pending) => current.Add(pending));
            });
    }
}