using System.Collections.Immutable;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections.Storage;

public interface IStorageBatchingStrategy
{
    Source<IImmutableList<PendingWrite>, ISourceQueueWithComplete<PendingWrite>> GetStrategy(
        Source<PendingWrite, ISourceQueueWithComplete<PendingWrite>> source);
}