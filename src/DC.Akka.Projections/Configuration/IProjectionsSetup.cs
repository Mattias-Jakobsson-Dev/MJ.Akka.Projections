using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionsSetup : IProjectionPartSetup<IProjectionsSetup>
{
    IProjectionsSetup WithProjection<TId, TDocument>(
        IProjection<TId, TDocument> projection,
        Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>> configure)
        where TId : notnull where TDocument : notnull;

    Task<ProjectionsApplication> Start();
    
    internal Func<Task<IActorRef>>? CoordinatorFactory { get; }
    internal Func<object, Task<IActorRef>>? ProjectionFactory { get; }
    internal RestartSettings? RestartSettings { get; }
    internal ProjectionStreamConfiguration ProjectionStreamConfiguration { get; }
    internal IProjectionStorage Storage { get; }
    internal IProjectionPositionStorage PositionStorage { get; }
}