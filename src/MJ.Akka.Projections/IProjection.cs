using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
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
}

public interface IProjection<TId, TContext, in TStorageSetup> : IProjection 
    where TId : notnull where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    ISetupProjectionHandlers<TId, TContext> Configure(ISetupProjection<TId, TContext> config);
    ILoadProjectionContext<TId, TContext> GetLoadProjectionContext(TStorageSetup storageSetup);
}