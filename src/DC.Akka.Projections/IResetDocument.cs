namespace DC.Akka.Projections;

public interface IResetDocument<out TDocument>
{
    TDocument Reset();
}