using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public interface IEventBatchingStrategy
{
    int GetParallelism();
    
    Source<ImmutableList<EventWithPosition>, NotUsed> Get(Source<EventWithPosition, NotUsed> source);
}