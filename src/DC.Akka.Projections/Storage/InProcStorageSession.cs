using System.Collections.Immutable;
using Akka.Actor;

namespace DC.Akka.Projections.Storage;

internal class InProcStorageSession(ProjectionsApplication application) : IStorageSession
{
    public async Task Store<TId, TDocument>(
        string projectionName,
        TId id,
        TDocument document,
        IActorRef ackTo,
        CancellationToken cancellationToken = default) where TId : notnull where TDocument : notnull
    {
        var storage = application
                          .GetProjectionConfiguration<TId, TDocument>(projectionName) ??
                      throw new NoDocumentProjectionException<TId, TDocument>(id);

        var transaction = await storage.DocumentStorage.StartTransaction(
            ImmutableList.Create<(TId Id, TDocument Document, IActorRef ackTo)>(
                (id, document, ackTo)),
            ImmutableArray<(TId id, IActorRef ackTo)>.Empty, 
            cancellationToken);

        await transaction.Commit(cancellationToken);
    }

    public async Task Delete<TId, TDocument>(
        string projectionName,
        TId id,
        IActorRef ackTo,
        CancellationToken cancellationToken = default)  where TId : notnull where TDocument : notnull
    {
        var storage = application
                          .GetProjectionConfiguration<TId, TDocument>(projectionName) ??
                      throw new NoDocumentProjectionException<TId, TDocument>(id);

        var transaction = await storage.DocumentStorage.StartTransaction(
            ImmutableList<(TId Id, TDocument Document, IActorRef ackTo)>.Empty,
            ImmutableList.Create(
                (id, ackTo)),
            cancellationToken);

        await transaction.Commit(cancellationToken);
    }
}