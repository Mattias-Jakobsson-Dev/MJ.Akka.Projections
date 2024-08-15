namespace DC.Akka.Projections.Configuration;

public interface IStartProjectionCoordinator
{
    Task IncludeProjection<TId, TDocument>(ProjectionConfiguration<TId, TDocument> configuration)
        where TId : notnull where TDocument : notnull;
    
    Task<IProjectionProxy?> GetCoordinatorFor(IProjection projection);

    Task Start();
}