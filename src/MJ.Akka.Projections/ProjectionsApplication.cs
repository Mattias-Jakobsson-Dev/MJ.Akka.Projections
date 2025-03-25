using System.Collections.Immutable;
using Akka.Actor;

namespace MJ.Akka.Projections;

public class ProjectionsApplication(IImmutableDictionary<string, IProjectionProxy> projections) 
    : ExtensionIdProvider<ProjectionsApplication>, IExtension
{
    public IImmutableDictionary<string, IProjectionProxy> GetProjections()
    {
        return projections;
    }
    
    public IProjectionProxy? GetProjection(string projectionName)
    {
        return projections.GetValueOrDefault(projectionName);
    }
    
    public override ProjectionsApplication CreateExtension(ExtendedActorSystem system)
    {
        return this;
    }
}