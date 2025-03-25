using System.Collections.Immutable;

namespace MJ.Akka.Projections.Configuration;

public interface IProjectorProxy
{
    Task<Messages.IProjectEventsResponse> ProjectEvents(
        ImmutableList<EventWithPosition> events,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    Task StopAllInProgress(TimeSpan timeout);
}