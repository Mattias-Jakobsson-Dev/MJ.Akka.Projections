using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public interface IEventBatchingStrategy
{
    int GetParallelism();
    
    Source<IEnumerable<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source);
}