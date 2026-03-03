namespace MJ.Akka.Projections;

public interface IEventWithAck
{
    Task Ack(CancellationToken cancellationToken);
    Task Nack(Exception error, CancellationToken cancellationToken);
}