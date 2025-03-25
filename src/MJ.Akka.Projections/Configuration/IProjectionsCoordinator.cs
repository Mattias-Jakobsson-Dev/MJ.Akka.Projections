namespace MJ.Akka.Projections.Configuration;

public interface IProjectionsCoordinator
{
    IProjectionProxy? Get(string projectionName);
}