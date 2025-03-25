namespace MJ.Akka.Projections.Configuration;

public interface IKeepTrackOfProjectors
{
    Task<IProjectorProxy> GetProjector<TId, TDocument>(
        TId id,
        ProjectionConfiguration configuration)
        where TId : notnull where TDocument : notnull;
}