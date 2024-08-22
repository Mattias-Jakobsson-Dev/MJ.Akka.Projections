using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public class NoBatchingPositionStrategy : IEventPositionBatchingStrategy
{
    public Source<PositionData, NotUsed> Get(Source<PositionData, NotUsed> source)
    {
        return source;
    }
}