namespace MJ.Akka.Projections;

public interface IResetDocument<out TDocument>
{
    TDocument Reset();
}