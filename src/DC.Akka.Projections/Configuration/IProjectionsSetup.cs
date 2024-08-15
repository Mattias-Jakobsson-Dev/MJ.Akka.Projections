using Akka.Streams;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionsSetup : IProjectionPartSetup<IProjectionsSetup>
{
    IProjectionsSetup WithProjection<TId, TDocument>(
        IProjection<TId, TDocument> projection,
        Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>>? configure = null)
        where TId : notnull where TDocument : notnull;

    Task<ProjectionsApplication> Start();
    
    internal IStartProjectionCoordinator CoordinatorFactory { get; }
    internal IKeepTrackOfProjectors ProjectionFactory { get; }
    internal RestartSettings? RestartSettings { get; }
    internal ProjectionStreamConfiguration ProjectionStreamConfiguration { get; }
    internal IProjectionStorage Storage { get; }
    internal IProjectionPositionStorage PositionStorage { get; }
}