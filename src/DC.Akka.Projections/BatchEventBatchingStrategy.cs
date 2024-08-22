using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public class BatchEventBatchingStrategy(int batchSize, int parallelism) : IEventBatchingStrategy
{
    public static BatchEventBatchingStrategy Default { get; } = new(1_000, 100);
    
    public int GetParallelism()
    {
        return parallelism;
    }

    public Source<IEnumerable<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source)
    {
        return source
            .Batch(
                batchSize,
                ImmutableList.Create,
                (current, item) => current.Add(item))
            .Select(x => (IEnumerable<EventWithPosition>)x);
    }
}