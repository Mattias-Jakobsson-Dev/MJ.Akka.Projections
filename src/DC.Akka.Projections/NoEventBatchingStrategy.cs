using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public class NoEventBatchingStrategy(int parallelism) : IEventBatchingStrategy
{
    public int GetParallelism()
    {
        return parallelism;
    }

    public Source<IEnumerable<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source)
    {
        return source
            .Select(x => (IEnumerable<EventWithPosition>)new List<EventWithPosition> { x });
    }
}