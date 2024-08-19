using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public static class ActorSystemExtensions
{
    public static async Task<IProjectionsCoordinator> StartProjections(
        this ActorSystem actorSystem,
        Func<IHaveConfiguration<ProjectionSystemConfiguration>, IHaveConfiguration<ProjectionSystemConfiguration>>
            configure)
    {
        var defaultConfiguration = ProjectionSystemConfiguration.CreateDefaultConfiguration(actorSystem);
        
        return await configure(new ConfigureProjectionSystem(actorSystem, defaultConfiguration)).Build();
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
