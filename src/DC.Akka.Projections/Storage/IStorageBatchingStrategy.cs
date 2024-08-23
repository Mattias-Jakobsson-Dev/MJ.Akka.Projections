using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public interface IStorageBatchingStrategy
{
    Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> GetStrategy(
        Source<IPendingWrite, ISourceQueueWithComplete<IPendingWrite>> source);
}