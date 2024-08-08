using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public class DocumentToDelete(object id, IActorRef ackTo) : StorageDocument(id, ackTo);