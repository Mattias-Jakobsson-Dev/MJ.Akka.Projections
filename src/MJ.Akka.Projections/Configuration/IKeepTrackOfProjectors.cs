namespace MJ.Akka.Projections.Configuration;

public interface IKeepTrackOfProjectors
{
    Task<IProjectorProxy> GetProjector(
        object id,
        ProjectionConfiguration configuration);

    IKeepTrackOfProjectors Reset();
}