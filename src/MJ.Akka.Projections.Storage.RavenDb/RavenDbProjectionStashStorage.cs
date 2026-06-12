using System.Collections.Immutable;
using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class RavenDbProjectionStashStorage(
    IDocumentStore documentStore,
    TimeSpan? inProcessTimeout = null) : IProjectionStashStorage
{
    private readonly TimeSpan _inProcessTimeout = inProcessTimeout ?? TimeSpan.FromMinutes(5);

    public async Task Stash(
        ProjectionContextId id,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var docId = ProjectionStash.BuildId(id);
        var stash = await session.LoadAsync<ProjectionStash>(docId, cancellationToken);

        if (stash == null)
        {
            stash = new ProjectionStash(id);
            await session.StoreAsync(stash, docId, cancellationToken);
        }

        foreach (var evnt in events)
        {
            stash.Events.Add(new StashedEvent(evnt.Event, evnt.Position));
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<(ImmutableList<EventWithPosition> Events, StashToken Token)> Unstash(
        ProjectionContextId id,
        uint? numberOfEvents = null,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var docId = ProjectionStash.BuildId(id);
        var stash = await session.LoadAsync<ProjectionStash>(docId, cancellationToken);

        if (stash == null || stash.Events.Count == 0)
            return (ImmutableList<EventWithPosition>.Empty, new StashToken(id, 0));

        // Auto-expire a stale in-process claim
        if (stash is { InProcessCount: > 0, InProcessClaimedAt: not null } &&
            DateTime.UtcNow - stash.InProcessClaimedAt.Value > _inProcessTimeout)
        {
            stash.InProcessCount = 0;
            stash.InProcessClaimedAt = null;
        }

        // Skip any events that are already checked out in another in-process batch
        var available = stash.Events.Skip(stash.InProcessCount).ToList();
        if (available.Count == 0)
            return (ImmutableList<EventWithPosition>.Empty, new StashToken(id, 0));

        var count = numberOfEvents == null || numberOfEvents >= available.Count
            ? available.Count
            : (int)numberOfEvents.Value;

        var result = available
            .Take(count)
            .Select(e => new EventWithPosition(e.Event, e.Position))
            .ToImmutableList();

        // Mark these events as in-process
        stash.InProcessCount += count;
        stash.InProcessClaimedAt = DateTime.UtcNow;
        await session.SaveChangesAsync(cancellationToken);

        return (result, new StashToken(id, (uint)count));
    }

    public async Task AckUnstash(StashToken token, CancellationToken cancellationToken = default)
    {
        if (token.Count == 0) return;

        using var session = documentStore.OpenAsyncSession();

        var docId = ProjectionStash.BuildId(token.Id);
        var stash = await session.LoadAsync<ProjectionStash>(docId, cancellationToken);

        if (stash == null) return;

        var removeCount = Math.Min((int)token.Count, stash.Events.Count);
        stash.Events.RemoveRange(0, removeCount);
        stash.InProcessCount = Math.Max(0, stash.InProcessCount - removeCount);
        if (stash.InProcessCount == 0)
            stash.InProcessClaimedAt = null;

        if (stash.Events.Count == 0)
            session.Delete(stash);

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task NackUnstash(StashToken token, CancellationToken cancellationToken = default)
    {
        if (token.Count == 0) return;

        using var session = documentStore.OpenAsyncSession();

        var docId = ProjectionStash.BuildId(token.Id);
        var stash = await session.LoadAsync<ProjectionStash>(docId, cancellationToken);

        if (stash == null) return;

        stash.InProcessCount = Math.Max(0, stash.InProcessCount - (int)token.Count);
        if (stash.InProcessCount == 0)
            stash.InProcessClaimedAt = null;

        await session.SaveChangesAsync(cancellationToken);
    }
}
