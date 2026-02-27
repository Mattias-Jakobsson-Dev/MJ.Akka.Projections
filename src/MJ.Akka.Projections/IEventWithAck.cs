namespace MJ.Akka.Projections;

public interface IEventWithAck
{
    Task Ack();
    Task Nack(Exception? exception = null);
}