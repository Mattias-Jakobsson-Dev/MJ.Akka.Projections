namespace MJ.Akka.Projections.Configuration;

public interface IConfigureProjectionCoordinator
{
    void WithProjection(IProjection projection);
    Task<IProjectionsCoordinator> Start();
}