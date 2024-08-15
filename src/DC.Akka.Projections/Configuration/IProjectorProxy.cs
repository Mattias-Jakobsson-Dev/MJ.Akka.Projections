using System.Collections.Immutable;

namespace DC.Akka.Projections.Configuration;

public interface IProjectorProxy
{
    Task<Messages.IProjectEventsResponse> ProjectEvents(IImmutableList<EventWithPosition> events);
}