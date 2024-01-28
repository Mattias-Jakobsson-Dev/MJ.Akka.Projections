using System.Collections.Immutable;

namespace DC.Akka.Projections;

public interface IProjectionSourceData
{
    IImmutableList<EventWithPosition> ParseEvents();
}