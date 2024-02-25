namespace DC.Akka.Projections.Storage;

public interface IStorageTransaction
{
    IStorageTransaction MergeWith(
        IStorageTransaction transaction,
        Func<IStorageTransaction, IStorageTransaction, IStorageTransaction> defaultMerge);
    
    Task Commit(CancellationToken cancellationToken = default);
}