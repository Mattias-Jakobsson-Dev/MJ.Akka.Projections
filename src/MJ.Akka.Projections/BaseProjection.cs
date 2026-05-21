using Akka.Actor;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections;

public abstract class BaseProjection<TIdContext, TContext, TStorageSetup> : IProjection<TIdContext, TContext, TStorageSetup>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    public virtual TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(30);
    
    public abstract ISetupProjection<TIdContext, TContext> Configure(ISetupProjection<TIdContext, TContext> config);
    
    public abstract ILoadProjectionContext<TIdContext, TContext> GetLoadProjectionContext(TStorageSetup storageSetup);

    public abstract TContext GetDefaultContext(TIdContext id);
    
    public abstract Task<IProjectionEventSource> GetSource();

    public virtual Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator.Init(configSupplier);
    }

    public Props CreateProjectionProps(ISupplyProjectionConfigurations configSupplier)
    {
        return DocumentProjection.Init(configSupplier);
    }

    public virtual long? GetInitialPosition() => null;

    public virtual string Name => GetType().Name;
}