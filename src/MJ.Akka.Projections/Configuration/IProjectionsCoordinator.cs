namespace MJ.Akka.Projections.Configuration;

public interface IProjectionsCoordinator : IAsyncDisposable
{
    IProjectionProxy? Get(string projectionName);
}