using System.Collections.Immutable;

namespace DC.Akka.Projections.Configuration;

public interface IProjectorProxy
{
    Task<Messages.IProjectEventsResponse> ProjectEvents(
        ImmutableList<EventWithPosition> events,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    void StopAllInProgress();
}