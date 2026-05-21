using Akka.Actor;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections;

public interface IProjection
{
    string Name { get; }
    TimeSpan ProjectionTimeout { get; }
    Task<IProjectionEventSource> GetSource();
    Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier);
    Props CreateProjectionProps(ISupplyProjectionConfigurations configSupplier);
    long? GetInitialPosition();
}

public interface IProjection<TIdContext, TContext, in TStorageSetup> : IProjection 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    ISetupProjection<TIdContext, TContext> Configure(ISetupProjection<TIdContext, TContext> config);
    ILoadProjectionContext<TIdContext, TContext> GetLoadProjectionContext(TStorageSetup storageSetup);
    TContext GetDefaultContext(TIdContext id);
}