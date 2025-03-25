using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections;

public class NoEventBatchingStrategy(int parallelism) : IEventBatchingStrategy
{
    public int GetParallelism()
    {
        return parallelism;
    }

    public Source<ImmutableList<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source)
    {
        return source
            .Select(ImmutableList.Create);
    }
}