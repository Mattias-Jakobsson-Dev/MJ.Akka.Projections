using Akka;
using Akka.Streams.Dsl;

namespace MJ.Akka.Projections;

public interface IProjectionEventSource
{
    Source<EventWithPosition, NotUsed> Start(long? fromPosition, CancellationToken cancellationToken);
}

public class SimpleProjectionEventSource(Func<long?, CancellationToken, Source<EventWithPosition, NotUsed>> start) : IProjectionEventSource
{
    public Source<EventWithPosition, NotUsed> Start(long? fromPosition, CancellationToken cancellationToken) 
        => start(fromPosition, cancellationToken);
}
