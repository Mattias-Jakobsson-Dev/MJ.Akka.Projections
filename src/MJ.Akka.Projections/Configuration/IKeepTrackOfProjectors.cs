using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Configuration;

public interface IKeepTrackOfProjectors
{
    Task<IProjectorProxy> GetProjector(
        IProjectionIdContext id,
        ProjectionConfiguration configuration);

    IKeepTrackOfProjectors Reset();
}