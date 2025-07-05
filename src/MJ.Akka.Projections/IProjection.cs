using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections;

public interface IProjection
{
    string Name { get; }
    TimeSpan ProjectionTimeout { get; }
    Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
    Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier);
    Props CreateProjectionProps(object id, ISupplyProjectionConfigurations configSupplier);
}

public interface IProjection<TId, TContext> : IProjection where TId : notnull where TContext : IProjectionContext<TId>
{
    TId IdFromString(string id);
    string IdToString(TId id);
    ISetupProjection<TId, TContext> Configure(ISetupProjection<TId, TContext> config);
}