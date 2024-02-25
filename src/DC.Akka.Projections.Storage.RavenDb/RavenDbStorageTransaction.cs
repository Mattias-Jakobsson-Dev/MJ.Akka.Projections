using System.Collections.Immutable;
using Akka.Actor;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace DC.Akka.Projections.Storage.RavenDb;

public record RavenDbStorageTransaction(
    IImmutableList<(string id, object document, IActorRef ackTo)> ToUpsert,
    IImmutableList<(string id, IActorRef ackTo)> ToDelete,
    IDocumentStore DocumentStore)
    : IStorageTransaction
{
    public IStorageTransaction MergeWith(
        IStorageTransaction transaction,
        Func<IStorageTransaction, IStorageTransaction, IStorageTransaction> defaultMerge)
    {
        if (transaction is RavenDbStorageTransaction otherTransaction)
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
        if (ToUpsert.Any())
        {
            try
            {
                var bulkInsert = DocumentStore.BulkInsert(new BulkInsertOptions
                {
                    SkipOverwriteIfUnchanged = true
                }, cancellationToken);

                foreach (var item in ToUpsert)
                    await bulkInsert.StoreAsync(item.document, item.id);

                await bulkInsert.DisposeAsync().ConfigureAwait(false);

                foreach (var item in ToUpsert)
                    item.ackTo.Tell(new Messages.Acknowledge());
            }
            catch (Exception e)
            {
                foreach (var item in ToUpsert)
                    item.ackTo.Tell(new Messages.Reject(e));
            }
        }

        if (ToDelete.Any())
        {
            try
            {
                using var session = DocumentStore.OpenAsyncSession();

                foreach (var item in ToDelete)
                    session.Delete(item.id);

                await session.SaveChangesAsync(cancellationToken);
                
                foreach (var item in ToDelete)
                    item.ackTo.Tell(new Messages.Acknowledge());
            }
            catch (Exception e)
            {
                foreach (var item in ToDelete)
                    item.ackTo.Tell(new Messages.Reject(e));
            }
        }
    }
}