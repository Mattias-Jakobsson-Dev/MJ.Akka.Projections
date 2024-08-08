using Akka.Actor;

namespace DC.Akka.Projections.Storage;

public abstract class StorageDocument(object id, IActorRef ackTo)
{
    public object Id { get; } = id;
    
    public void Ack()
    {
        ackTo.Tell(new Messages.Acknowledge());
    }

    public void Reject(Exception cause)
    {
        ackTo.Tell(new Messages.Reject(cause));
    }
}