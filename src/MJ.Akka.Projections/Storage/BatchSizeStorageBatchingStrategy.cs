using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage;

public class BatchSizeStorageBatchingStrategy(int batchSize) : IStorageBatchingStrategy
{
    public Source<IImmutableList<IPendingWrite>, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source)
    {
        return source
            .Batch(
                batchSize, 
                IImmutableList<IPendingWrite> (x) => ImmutableList.Create(x), 
                (current, pending) => current.Add(pending));
    }
}