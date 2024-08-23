using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public class BatchSizeStorageBatchingStrategy(int batchSize) : IStorageBatchingStrategy
{
    public Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source)
    {
        return source
            .Batch(
                batchSize,
                x => x,
                (current, pending) => current.MergeWith(pending));
    }
}