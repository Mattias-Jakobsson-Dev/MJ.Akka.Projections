using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public class BufferWithinStorageBatchingStrategy(int items, TimeSpan timeout) : IStorageBatchingStrategy
{
    public Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source)
    {
        return source
            .GroupedWithin(items, timeout)
            .Select(x =>
            {
                return x.Aggregate(
                    (IPendingWrite)PendingWrite.Empty,
                    (current, pending) => current.MergeWith(pending));
            });
    }
}