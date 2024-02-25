using System.Collections.Immutable;
using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public interface IProjectionStorage<TId, TDocument> where TId : notnull where TDocument : notnull
{
    Task<(TDocument? document, bool requireReload)> LoadDocument(TId id, CancellationToken cancellationToken = default);
    
    Task<IStorageTransaction> StartTransaction(
        IImmutableList<(TId Id, TDocument Document, IActorRef ackTo)> toUpsert,
        IImmutableList<(TId id, IActorRef ackTo)> toDelete,
        CancellationToken cancellationToken = default);
}