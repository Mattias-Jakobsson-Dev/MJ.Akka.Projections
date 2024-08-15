using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public interface IStartProjectionCoordinator
{
    Task<IActorRef> Start<TId, TDocument>(ProjectionConfiguration<TId, TDocument>  configuration) 
        where TId : notnull where TDocument : notnull;
}