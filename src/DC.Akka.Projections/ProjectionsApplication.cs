using System.Collections.Immutable;
using Akka.Actor;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

public class ProjectionsApplication(ActorSystem actorSystem, IImmutableDictionary<string, IProjectionProxy> projections) 
    : ExtensionIdProvider<ProjectionsApplication>, IExtension
{
    public ActorSystem ActorSystem => actorSystem;

    // public async Task<ProjectionsCoordinator<TId, TDocument>.Proxy> WithProjection<TId, TDocument>(
    //     IProjection<TId, TDocument> projection,
    //     Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>> configure)
    //     where TId : notnull where TDocument : notnull
    // {
    //     var configuration = configure(new ProjectionConfigurationSetup<TId, TDocument>(projection, this))
    //         .Build();
    //     
    //     _projections.AddOrUpdate(projection.Name, _ => configuration, (_, _) => configuration);
    //
    //     var coordinator = await configuration.CreateProjectionCoordinator();
    //
    //     return new ProjectionsCoordinator<TId, TDocument>.Proxy(coordinator);
    // }
    //

    public IProjectionProxy? GetProjection(string projectionName)
    {
        return projections.GetValueOrDefault(projectionName);
    }
    
    public override ProjectionsApplication CreateExtension(ExtendedActorSystem system)
    {
        return this;
    }
}