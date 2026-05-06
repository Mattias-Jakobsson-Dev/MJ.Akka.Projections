using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage;

[PublicAPI]
public record StashToken(ProjectionContextId Id, uint Count);

[PublicAPI]
public interface IProjectionStashStorage
{
    Task Stash(
        ProjectionContextId id,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken = default);

    Task<(ImmutableList<EventWithPosition> Events, StashToken Token)> Unstash(
        ProjectionContextId id,
        uint? numberOfEvents = null,
        CancellationToken cancellationToken = default);

    Task AckUnstash(
        StashToken token,
        CancellationToken cancellationToken = default);

    Task NackUnstash(
        StashToken token,
        CancellationToken cancellationToken = default);
}
