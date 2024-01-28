using System.Collections.Concurrent;
using Akka.Actor;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections;

internal class ProjectionsApplication : ExtensionIdProvider<ProjectionsApplication>, IExtension
{
    private readonly ConcurrentDictionary<string, object> _projections = new();

    public void WithProjection<TDocument>(string name, ProjectionConfiguration<TDocument> configuration)
    {
        _projections.AddOrUpdate(name, _ => configuration, (_, _) => configuration);
    }
    
    public ProjectionConfiguration<TDocument>? GetProjectionConfiguration<TDocument>(string name)
    {
        return _projections.GetValueOrDefault(name) as ProjectionConfiguration<TDocument>;
    }
    
    public override ProjectionsApplication CreateExtension(ExtendedActorSystem system)
    {
        return this;
    }
}