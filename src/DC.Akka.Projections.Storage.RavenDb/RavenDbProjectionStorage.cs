using System.Collections.Immutable;
using Akka.Actor;
using Raven.Client.Documents;

namespace DC.Akka.Projections.Storage.RavenDb;

public class RavenDbProjectionStorage<TDocument>(IDocumentStore documentStore) 
    : IProjectionStorage<string, TDocument> where TDocument : notnull
{
    public async Task<(TDocument? document, bool requireReload)> LoadDocument(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        return (await session.LoadAsync<TDocument>(id, cancellationToken), true);
    }

    public Task<IStorageTransaction> StartTransaction(
        IImmutableList<(string Id, TDocument Document, IActorRef ackTo)> toUpsert,
        IImmutableList<(string id, IActorRef ackTo)> toDelete,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IStorageTransaction>(new RavenDbStorageTransaction(
            toUpsert.Select(x => (x.Id, (object)x.Document, x.ackTo)).ToImmutableList(),
            toDelete.Select(x => (x.id, x.ackTo)).ToImmutableList(),
            documentStore));
    }
}