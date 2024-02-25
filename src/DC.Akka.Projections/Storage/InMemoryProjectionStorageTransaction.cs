using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;

namespace DC.Akka.Projections.Storage;

internal record InMemoryProjectionStorageTransaction<TId, TDocument>(
    IImmutableList<(TId id, TDocument document, IActorRef ackTo)> ToUpsert,
    IImmutableList<(TId id, IActorRef ackTo)> ToDelete,
    ConcurrentDictionary<TId, (Type Type, ReadOnlyMemory<byte> Data)> Documents,
    Func<object, Task<ReadOnlyMemory<byte>>> SerializeData) 
    : IStorageTransaction where TId : notnull
{
    public IStorageTransaction MergeWith(
        IStorageTransaction transaction,
        Func<IStorageTransaction, IStorageTransaction, IStorageTransaction> defaultMerge)
    {
        if (transaction is InMemoryProjectionStorageTransaction<TId, TDocument> otherTransaction)
        {
            return this with
            {
                ToUpsert = ToUpsert.AddRange(otherTransaction.ToUpsert),
                ToDelete = ToDelete.AddRange(otherTransaction.ToDelete)
            };
        }
        
        return defaultMerge(this, transaction);
    }

    public async Task Commit(CancellationToken cancellationToken = default)
    {
        foreach (var document in ToUpsert)
        {
            var serialized = await SerializeData(document.document!);

            var result = (document.document!.GetType(), serialized);
            
            Documents.AddOrUpdate(document.id, _ => result, (_, _) => result);
            
            document.ackTo.Tell(new Messages.Acknowledge());
        }

        foreach (var document in ToDelete)
        {
            Documents.TryRemove(document.id, out _);

            document.ackTo.Tell(new Messages.Acknowledge());
        }
    }
}