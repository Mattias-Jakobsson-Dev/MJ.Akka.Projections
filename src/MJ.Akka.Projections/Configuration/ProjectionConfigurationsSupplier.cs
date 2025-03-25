using System.Collections.Immutable;
using Akka.Actor;

namespace MJ.Akka.Projections.Configuration;

public class ProjectionConfigurationsSupplier : IExtension
{
    private readonly IImmutableDictionary<string, ProjectionConfiguration> _configuredProjections;

    private ProjectionConfigurationsSupplier(
        IImmutableDictionary<string, ProjectionConfiguration> configuredProjections)
    {
        _configuredProjections = configuredProjections;
    }

    public ProjectionConfiguration GetConfigurationFor(string projectionName)
    {
        return _configuredProjections.TryGetValue(projectionName, out var config)
            ? config
            : throw new NoDocumentProjectionException(projectionName);
    }

    internal static void Register(
        ActorSystem actorSystem,
        IImmutableDictionary<string, ProjectionConfiguration> projectionConfigurations)
    {
        actorSystem.RegisterExtension(new Provider(new ProjectionConfigurationsSupplier(projectionConfigurations)));
    }

    private class Provider(ProjectionConfigurationsSupplier supplier)
        : ExtensionIdProvider<ProjectionConfigurationsSupplier>
    {
        public override ProjectionConfigurationsSupplier CreateExtension(ExtendedActorSystem system)
        {
            return supplier;
        }
    }
}