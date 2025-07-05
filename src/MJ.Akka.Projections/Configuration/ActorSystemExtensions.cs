using Akka.Actor;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public static class ActorSystemExtensions
{
    public static IConfigureProjectionCoordinator Projections<TStorageSetup>(
        this ActorSystem actorSystem,
        Func<IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>>,
                IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>>>
            configure,
        TStorageSetup storageSetup)
        where TStorageSetup : IStorageSetup
    {
        var defaultConfiguration = ProjectionSystemConfiguration<TStorageSetup>
            .CreateDefaultConfiguration(actorSystem, storageSetup);

        return configure(
            new ConfigureProjectionSystem<TStorageSetup>(actorSystem, defaultConfiguration)).Build();
    }

    private record ConfigureProjectionSystem<TStorageSetup>(
        ActorSystem ActorSystem,
        ProjectionSystemConfiguration<TStorageSetup> Config)
        : IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>>
        where TStorageSetup : IStorageSetup
    {
        public IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> WithModifiedConfig(
            Func<ProjectionSystemConfiguration<TStorageSetup>, ProjectionSystemConfiguration<TStorageSetup>> modify)
        {
            return this with
            {
                Config = modify(Config)
            };
        }
    }
}