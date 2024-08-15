namespace DC.Akka.Projections.Configuration;

public interface IKeepTrackOfProjectors
{
    Task<IProjectorProxy> GetProjector<TId, TDocument>(
        TId id,
        ProjectionConfiguration<TId, TDocument> configuration)
        where TId : notnull where TDocument : notnull;
}