using Akka.Actor;

namespace MJ.Akka.Projections.Configuration;

public static class ActorSystemExtensions
{
    public static IConfigureProjectionCoordinator Projections(
        this ActorSystem actorSystem,
        Func<IHaveConfiguration<ProjectionSystemConfiguration>, IHaveConfiguration<ProjectionSystemConfiguration>>
            configure)
    {
        var defaultConfiguration = ProjectionSystemConfiguration.CreateDefaultConfiguration(actorSystem);
        
        return configure(new ConfigureProjectionSystem(actorSystem, defaultConfiguration)).Build();
    }

    private record ConfigureProjectionSystem(
        ActorSystem ActorSystem,
        ProjectionSystemConfiguration Config)
        : IHaveConfiguration<ProjectionSystemConfiguration>
    {
        public IHaveConfiguration<ProjectionSystemConfiguration> WithModifiedConfig(
            Func<ProjectionSystemConfiguration, ProjectionSystemConfiguration> modify)
        {
            return this with
            {
                Config = modify(Config)
            };
        }
    }
}
