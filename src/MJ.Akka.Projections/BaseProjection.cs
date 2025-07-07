using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections;

public abstract class BaseProjection<TId, TContext, TStorageSetup> : IProjection<TId, TContext, TStorageSetup>
    where TId : notnull where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    public virtual TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(30);
    
    public abstract ISetupProjectionHandlers<TId, TContext> Configure(ISetupProjection<TId, TContext> config);
    
    public abstract ILoadProjectionContext<TId, TContext> GetLoadProjectionContext(TStorageSetup storageSetup);

    public abstract Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
    
    public virtual Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator.Init(configSupplier);
    }

    public Props CreateProjectionProps(ISupplyProjectionConfigurations configSupplier)
    {
        return DocumentProjection.Init(configSupplier);
    }

    public virtual string Name => GetType().Name;
}