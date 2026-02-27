using System.Collections.Immutable;
using Akka.Actor;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Configuration;

public class ActorRefProjectorProxy(IProjectionIdContext id, IActorRef projector) : IProjectorProxy
{
    public Task<Messages.IProjectEventsResponse> ProjectEvents(
        ImmutableList<EventWithPosition> events,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return projector.Ask<Messages.IProjectEventsResponse>(
            new DocumentProjection.Commands.ProjectEvents(id, events),
            timeout,
            cancellationToken);
    }

    public async Task StopAllInProgress(TimeSpan timeout)
    {
        var response = await projector.Ask<DocumentProjection.Responses.StopInProcessEventsResponse>(
            new DocumentProjection.Commands.StopInProcessEvents(id),
            timeout);

        if (response.Error is not null)
            throw response.Error;
    }
}