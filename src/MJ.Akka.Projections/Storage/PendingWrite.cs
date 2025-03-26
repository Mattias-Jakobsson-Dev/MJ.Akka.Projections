using System.Collections.Immutable;
using Akka;

namespace MJ.Akka.Projections.Storage;

internal class PendingWrite : IPendingWrite
{
    private readonly IImmutableList<TaskCompletionSource<NotUsed>> _completions;

    public static PendingWrite Empty { get; } = new(
        ImmutableList<DocumentToStore>.Empty,
        ImmutableList<DocumentToDelete>.Empty,
        ImmutableList<TaskCompletionSource<NotUsed>>.Empty,
        CancellationToken.None);

    public PendingWrite(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        TaskCompletionSource<NotUsed> completion,
        CancellationToken cancellationToken) : this(
        toUpsert,
        toDelete,
        ImmutableList.Create(completion),
        cancellationToken)
    {
    }

    private PendingWrite(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        IImmutableList<TaskCompletionSource<NotUsed>> completions,
        CancellationToken cancellationToken)
    {
        ToUpsert = toUpsert;
        ToDelete = toDelete;
        _completions = completions;
        CancellationToken = cancellationToken;
    }
    
    public IImmutableList<DocumentToStore> ToUpsert { get; }
    public IImmutableList<DocumentToDelete> ToDelete { get; }
    public CancellationToken CancellationToken { get; }
    
    public bool IsEmpty => ToUpsert.Count == 0 && ToDelete.Count == 0 && _completions.Count == 0;

    public IPendingWrite MergeWith(IPendingWrite other)
    {
        var cancellationToken = CancellationToken == other.CancellationToken
            ? CancellationToken
            : CancellationTokenSource
                .CreateLinkedTokenSource(CancellationToken, other.CancellationToken)
                .Token;
        
        return new PendingWrite(
            Merge(ToUpsert, other.ToUpsert),
            Merge(ToDelete, other.ToDelete),
            _completions.AddRange(other.GetCompletions()),
            cancellationToken);
    }

    public void Completed()
    {
        foreach (var completion in _completions)
            completion.TrySetResult(NotUsed.Instance);
    }

    public void Fail(Exception exception)
    {
        foreach (var completion in _completions)
            completion.TrySetException(exception);
    }

    public IImmutableList<TaskCompletionSource<NotUsed>> GetCompletions()
    {
        return _completions;
    }

    private static ImmutableList<T> Merge<T>(
        IImmutableList<T> existing,
        IImmutableList<T> newItems) where T : StorageDocument<T>
    {
        return existing
            .AddRange(newItems)
            .Aggregate(
                ImmutableList<T>.Empty,
                (current, item) =>
                {
                    var existingItem = current
                        .FirstOrDefault(x => x.Equals(item));

                    return existingItem != null
                        ? current.Remove(existingItem).Add(item)
                        : current.Add(item);
                });
    }
}