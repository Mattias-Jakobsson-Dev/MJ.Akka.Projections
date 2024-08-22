using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public class BatchWithinEventPositionBatchingStrategy(int maxItems, TimeSpan timeout) : IEventPositionBatchingStrategy
{
    public static BatchWithinEventPositionBatchingStrategy Default { get; } = new(10_000, TimeSpan.FromSeconds(10.0));
    
    public Source<PositionData, NotUsed> Get(Source<PositionData, NotUsed> source)
    {
        return source
            .GroupedWithin(maxItems, timeout)
            .Select(positions =>
            {
                var highestPosition = positions.Select(x => x.Position).MaxBy(y => y);

                return new PositionData(highestPosition);
            });
    }
}