using System.Collections.Immutable;
using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public class ActorRefProjectorProxy<TId, TDocument>(TId id, IActorRef projector) : IProjectorProxy
    where TId : notnull where TDocument : notnull
{
    public Task<Messages.IProjectEventsResponse> ProjectEvents(
        ImmutableList<EventWithPosition> events,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return projector.Ask<Messages.IProjectEventsResponse>(
            new DocumentProjection<TId, TDocument>.Commands.ProjectEvents(id, events),
            timeout,
            cancellationToken);
    }

    public async Task StopAllInProgress(TimeSpan timeout)
    {
        var response = await projector.Ask<DocumentProjection<TId, TDocument>.Responses.StopInProcessEventsResponse>(
            new DocumentProjection<TId, TDocument>.Commands.StopInProcessEvents(id),
            timeout);

        if (response.Error is not null)
            throw response.Error;
    }
}