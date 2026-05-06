using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionStashStorage(TimeSpan? inProcessTimeout = null) : IProjectionStashStorage
{
    private readonly TimeSpan _inProcessTimeout = inProcessTimeout ?? TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<ProjectionContextId, List<EventWithPosition>> _stash = new();
    private readonly ConcurrentDictionary<StashToken, (ImmutableList<EventWithPosition> Events, DateTime ClaimedAt)> _inProgress = new();

    public Task Stash(
        ProjectionContextId id,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken = default)
    {
        var list = _stash.GetOrAdd(id, _ => []);
        lock (list)
        {
            list.AddRange(events);
        }
        return Task.CompletedTask;
    }

    public Task<(ImmutableList<EventWithPosition> Events, StashToken Token)> Unstash(
        ProjectionContextId id,
        uint? numberOfEvents = null,
        CancellationToken cancellationToken = default)
    {
        if (!_stash.TryGetValue(id, out var list))
        {
            var emptyToken = new StashToken(id, 0);
            return Task.FromResult((ImmutableList<EventWithPosition>.Empty, emptyToken));
        }

        lock (list)
        {
            if (list.Count == 0)
            {
                var emptyToken = new StashToken(id, 0);
                return Task.FromResult((ImmutableList<EventWithPosition>.Empty, emptyToken));
            }

            // Auto-expire stale in-progress entries for this id
            var staleTokens = _inProgress
                .Where(kv => kv.Key.Id.Equals(id) && DateTime.UtcNow - kv.Value.ClaimedAt > _inProcessTimeout)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var stale in staleTokens)
                _inProgress.TryRemove(stale, out _);

            // Count how many events at the front are still legitimately in-process
            var inProcessCount = _inProgress
                .Where(kv => kv.Key.Id.Equals(id))
                .Sum(kv => (int)kv.Key.Count);

            var available = list.Skip(inProcessCount).ToList();
            if (available.Count == 0)
            {
                var emptyToken = new StashToken(id, 0);
                return Task.FromResult((ImmutableList<EventWithPosition>.Empty, emptyToken));
            }

            ImmutableList<EventWithPosition> result;
            if (numberOfEvents == null || numberOfEvents >= available.Count)
                result = available.ToImmutableList();
            else
                result = available.Take((int)numberOfEvents.Value).ToImmutableList();

            var token = new StashToken(id, (uint)result.Count);
            _inProgress[token] = (result, DateTime.UtcNow);

            return Task.FromResult((result, token));
        }
    }

    public Task AckUnstash(StashToken token, CancellationToken cancellationToken = default)
    {
        if (!_inProgress.TryRemove(token, out _))
            return Task.CompletedTask;

        if (!_stash.TryGetValue(token.Id, out var list))
            return Task.CompletedTask;

        lock (list)
        {
            list.RemoveRange(0, Math.Min((int)token.Count, list.Count));
        }

        return Task.CompletedTask;
    }

    public Task NackUnstash(StashToken token, CancellationToken cancellationToken = default)
    {
        _inProgress.TryRemove(token, out _);
        return Task.CompletedTask;
    }
}
