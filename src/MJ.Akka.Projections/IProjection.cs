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

public interface IProjection<TId, TDocument> : IProjection where TId : notnull where TDocument : notnull
{
    TId IdFromString(string id);
    string IdToString(TId id);
    ISetupProjection<TId, TDocument> Configure(ISetupProjection<TId, TDocument> config);
}