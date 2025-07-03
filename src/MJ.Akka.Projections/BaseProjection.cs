using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections;

public abstract class BaseProjection<TId, TDocument> : IProjection<TId, TDocument>
    where TId : notnull where TDocument : notnull
{
    public virtual TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(30);
    
    public abstract TId IdFromString(string id);
    public abstract string IdToString(TId id);

    public abstract ISetupProjection<TId, TDocument> Configure(ISetupProjection<TId, TDocument> config);
    public abstract Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
    
    public virtual Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator<TId, TDocument>.Init(configSupplier);
    }

    public Props CreateProjectionProps(object id, ISupplyProjectionConfigurations configSupplier)
    {
        return DocumentProjection<TId, TDocument>.Init((TId)id, configSupplier);
    }

    public virtual string Name => GetType().Name;
}