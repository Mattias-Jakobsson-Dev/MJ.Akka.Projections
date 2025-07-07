using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage.Batched;

public class BatchSizeStorageBatchingStrategy(int batchSize) : IStorageBatchingStrategy
{
    public Source<IImmutableList<PendingWrite>, ISourceQueueWithComplete<PendingWrite>> 
        GetStrategy(Source<PendingWrite, ISourceQueueWithComplete<PendingWrite>> source)
    {
        return source
            .Batch(
                batchSize, 
                IImmutableList<PendingWrite> (x) => ImmutableList.Create(x), 
                (current, pending) => current.Add(pending));
    }
}