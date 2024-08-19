namespace DC.Akka.Projections.Configuration;

public interface IConfigureProjectionCoordinator
{
    Task WithProjection(IProjection projection);
    Task<IProjectionsCoordinator> Start();
}