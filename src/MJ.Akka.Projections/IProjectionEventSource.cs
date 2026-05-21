using Akka;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections;

public interface IProjectionEventSource
{
    Source<EventWithPosition, NotUsed> Start(long? fromPosition);
}

public class SimpleProjectionEventSource(Func<long?, Source<EventWithPosition, NotUsed>> start) : IProjectionEventSource
{
    public Source<EventWithPosition, NotUsed> Start(long? fromPosition) => start(fromPosition);
}
