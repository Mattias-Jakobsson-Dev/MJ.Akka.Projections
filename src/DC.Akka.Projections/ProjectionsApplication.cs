using System.Collections.Concurrent;
using Akka.Actor;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections;

public class ProjectionsApplication(ActorSystem actorSystem) : IExtension
{
    private readonly ConcurrentDictionary<string, object> _projections = new();

    public ActorSystem ActorSystem => actorSystem;

    public async Task<ProjectionsCoordinator<TId, TDocument>.Proxy> WithProjection<TId, TDocument>(
        IProjection<TId, TDocument> projection,
        Func<IProjectionConfigurationSetup<TId, TDocument>, IProjectionConfigurationSetup<TId, TDocument>> configure)
        where TId : notnull where TDocument : notnull
    {
        var configuration = configure(new ProjectionConfigurationSetup<TId, TDocument>(projection, this))
            .Build();
        
        _projections.AddOrUpdate(projection.Name, _ => configuration, (_, _) => configuration);

        var coordinator = await configuration.CreateProjectionCoordinator();

        return new ProjectionsCoordinator<TId, TDocument>.Proxy(coordinator);
    }
    
    public ProjectionConfiguration<TId, TDocument>? GetProjectionConfiguration<TId, TDocument>(string name)
        where TId : notnull where TDocument : notnull
    {
        return _projections.GetValueOrDefault(name) as ProjectionConfiguration<TId, TDocument>;
    }
    
    internal class Provider : ExtensionIdProvider<ProjectionsApplication>
    {
        public override ProjectionsApplication CreateExtension(ExtendedActorSystem system)
        {
            return new ProjectionsApplication(system);
        }
    }
}