using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections;

public class BatchEventBatchingStrategy(int batchSize, int parallelism) : IEventBatchingStrategy
{
    public static BatchEventBatchingStrategy Default { get; } = new(1_000, 100);
    
    public int GetParallelism()
    {
        return parallelism;
    }

    public Source<ImmutableList<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source)
    {
        return source
            .Batch(
                batchSize,
                ImmutableList.Create,
                (current, item) => current.Add(item));
    }
}