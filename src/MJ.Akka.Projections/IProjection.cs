using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections;

public interface IProjection
{
    string Name { get; }
    TimeSpan ProjectionTimeout { get; }
    Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
    Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier);
    Props CreateProjectionProps(ISupplyProjectionConfigurations configSupplier);
    long? GetInitialPosition();
}

public interface IProjection<TIdContext, TContext, in TStorageSetup> : IProjection 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    ISetupProjectionHandlers<TIdContext, TContext> Configure(ISetupProjection<TIdContext, TContext> config);
    ILoadProjectionContext<TIdContext, TContext> GetLoadProjectionContext(TStorageSetup storageSetup);
    TContext GetDefaultContext(TIdContext id);
}