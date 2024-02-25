using System.Collections.Immutable;

namespace DC.Akka.Projections.Storage;

public record MergedTransactions(IImmutableList<IStorageTransaction> InnerTransactions) : IStorageTransaction
{
    public IStorageTransaction MergeWith(
        IStorageTransaction transaction,
        Func<IStorageTransaction, IStorageTransaction, IStorageTransaction> defaultMerge)
    {
        return defaultMerge(this, transaction);
    }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(InnerTransactions.Select(x => x.Commit(cancellationToken)));
    }

    public static IStorageTransaction Create(IStorageTransaction first, IStorageTransaction second)
    {
        if (second is MergedTransactions secondMerged)
        {
            return secondMerged.InnerTransactions.Aggregate(
                first,
                (current, innerTransaction) => current.MergeWith(innerTransaction, Create));
        }
        
        if (first is not MergedTransactions firstMerged)
            return new MergedTransactions(ImmutableList.Create(first, second));

        var results = ImmutableList<IStorageTransaction>.Empty;
        
        foreach (var innerTransaction in firstMerged.InnerTransactions)
        {
            var mergeResult = innerTransaction.MergeWith(second, Create);

            if (mergeResult is MergedTransactions)
            {
                results = results.Add(innerTransaction);
            }
            else
            {
                second = mergeResult;
            }
        }

        results = results.Add(second);

        return new MergedTransactions(results);
    }
}