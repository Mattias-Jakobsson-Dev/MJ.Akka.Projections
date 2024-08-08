using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public class DocumentToStore(object id, object document, IActorRef ackTo) : StorageDocument(id, ackTo)
{
    public object Document { get; } = document;
}