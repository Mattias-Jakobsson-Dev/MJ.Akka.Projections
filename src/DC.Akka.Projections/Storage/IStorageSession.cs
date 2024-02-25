using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public interface IStorageSession
{
    Task Store<TId, TDocument>(
        string projectionName,
        TId id, 
        TDocument document,
        IActorRef ackTo,
        CancellationToken cancellationToken = default) where TId : notnull where TDocument : notnull;
    
    Task Delete<TId, TDocument>(
        string projectionName,
        TId id, 
        IActorRef ackTo,
        CancellationToken cancellationToken = default) where TId : notnull where TDocument : notnull;
}