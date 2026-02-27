using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage;

public class PendingWrite
{
    private readonly IImmutableList<TaskCompletionSource> _completions;

    public static PendingWrite Empty { get; } = new(
        ImmutableDictionary<ProjectionContextId, IProjectionContext>.Empty,
        ImmutableList<TaskCompletionSource>.Empty,
        CancellationToken.None);

    public PendingWrite(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        TaskCompletionSource completion,
        CancellationToken cancellationToken) : this(
        contexts,
        ImmutableList.Create(completion),
        cancellationToken)
    {
    }

    private PendingWrite(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        IImmutableList<TaskCompletionSource> completions,
        CancellationToken cancellationToken)
    {
        Contexts = contexts;
        _completions = completions;
        CancellationToken = cancellationToken;
    }

    public IImmutableDictionary<ProjectionContextId, IProjectionContext> Contexts { get; }
    public CancellationToken CancellationToken { get; }

    public bool IsEmpty => !Contexts.Any() && _completions.Count == 0;

    public PendingWrite MergeWith(PendingWrite other)
    {
        var cancellationToken = CancellationToken == other.CancellationToken
            ? CancellationToken
            : CancellationTokenSource
                .CreateLinkedTokenSource(CancellationToken, other.CancellationToken)
                .Token;

        var result = other
            .Contexts
            .Aggregate(
                Contexts,
                (current, projectionContext) => current
                    .SetItem(projectionContext.Key, current.TryGetValue(projectionContext.Key, out var existingContext)
                        ? existingContext.MergeWith(projectionContext.Value)
                        : projectionContext.Value));

        return new PendingWrite(
            result,
            _completions.AddRange(other.GetCompletions()),
            cancellationToken);
    }

    public void Completed()
    {
        foreach (var completion in _completions)
            completion.TrySetResult();
    }

    public void Fail(Exception exception)
    {
        foreach (var completion in _completions)
            completion.TrySetException(exception);
    }

    private IImmutableList<TaskCompletionSource> GetCompletions()
    {
        return _completions;
    }
}