using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage;

public interface IStorageBatchingStrategy
{
    Source<IImmutableList<IPendingWrite>, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source);
}