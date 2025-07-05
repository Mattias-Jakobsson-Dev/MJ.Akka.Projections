using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections;

public abstract class BaseProjection<TId, TContext> : IProjection<TId, TContext>
    where TId : notnull where TContext : IProjectionContext<TId>
{
    public virtual TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(30);
    
    public abstract TId IdFromString(string id);
    public abstract string IdToString(TId id);

    public abstract ISetupProjection<TId, TContext> Configure(ISetupProjection<TId, TContext> config);
    public abstract Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
    
    public virtual Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator.Init(configSupplier);
    }

    public Props CreateProjectionProps(object id, ISupplyProjectionConfigurations configSupplier)
    {
        return DocumentProjection.Init((TId)id, configSupplier);
    }

    public virtual string Name => GetType().Name;
}