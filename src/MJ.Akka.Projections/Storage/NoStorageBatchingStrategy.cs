using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage;

public class NoStorageBatchingStrategy : IStorageBatchingStrategy
{
    public Source<IImmutableList<PendingWrite>, ISourceQueueWithComplete<PendingWrite>>
        GetStrategy(
            Source<PendingWrite, ISourceQueueWithComplete<PendingWrite>> source)
    {
        return source
            .Select(IImmutableList<PendingWrite> (x) => ImmutableList.Create(x));
    }
}