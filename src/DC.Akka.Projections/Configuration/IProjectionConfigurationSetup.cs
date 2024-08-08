using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionConfigurationSetup<TId, TDocument> 
    : IProjectionPartSetup<IProjectionConfigurationSetup<TId, TDocument>> 
    where TId : notnull where TDocument : notnull
{
    IProjection<TId, TDocument> Projection { get; }
    
    internal ProjectionConfiguration<TId, TDocument> Build(IProjectionsSetup setup);
}