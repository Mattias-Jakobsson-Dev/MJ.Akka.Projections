namespace MJ.Akka.Projections.Configuration;

public interface IConfigureProjectionCoordinator
{
    void WithProjection(ProjectionConfiguration projection);
    Task<IProjectionsCoordinator> Start();
}