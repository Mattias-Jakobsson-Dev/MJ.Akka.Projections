namespace MJ.Akka.Projections;

public interface IResettableProjectionContext : IProjectionContext
{
    IProjectionContext Reset();
}