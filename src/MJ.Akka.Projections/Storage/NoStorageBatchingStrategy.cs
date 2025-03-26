using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage;

public class NoStorageBatchingStrategy : IStorageBatchingStrategy
{
    public Source<IImmutableList<IPendingWrite>, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source)
    {
        return source
            .Select(x => (IImmutableList<IPendingWrite>)ImmutableList.Create(x));
    }
}