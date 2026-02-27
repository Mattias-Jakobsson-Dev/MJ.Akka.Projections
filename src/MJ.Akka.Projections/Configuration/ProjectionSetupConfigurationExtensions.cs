using Akka.Actor;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public static class ProjectionSetupConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> WithProjection<TIdContext, TContext, TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> source,
        IProjection<TIdContext, TContext, TStorageSetup> projection,
        Func<IHaveConfiguration<ProjectionInstanceConfiguration>, IHaveConfiguration<ProjectionInstanceConfiguration>>?
            configure = null)
        where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
    {
        return source
            .WithModifiedConfig(x => x with
            {
                Projections = x.Projections.SetItem(
                    projection.Name,
                    conf =>
                    {
                        var configuredProjection = (configure ?? (c => c))(new ConfigureProjection(
                                source.ActorSystem,
                                ProjectionInstanceConfiguration.Empty))
                            .Config
                            .MergeWith(conf);
                        
                        IStorageSetup storageSetup = conf.StorageSetup;

                        foreach (var modifier in conf.StorageModifiers)
                        {
                            storageSetup = modifier.Modify(storageSetup);
                        }

                        return new ProjectionConfiguration<TIdContext, TContext, TStorageSetup>(
                            projection,
                            storageSetup.CreateProjectionStorage(),
                            projection.GetLoadProjectionContext(conf.StorageSetup),
                            storageSetup.CreatePositionStorage(),
                            conf.ProjectorFactory,
                            configuredProjection.RestartSettings,
                            configuredProjection.EventBatchingStrategy!,
                            configuredProjection.PositionBatchingStrategy!,
                            projection.Configure(new SetupProjection<TIdContext, TContext>()).Build());
                    })
            });
    }

    private record ConfigureProjection(ActorSystem ActorSystem, ProjectionInstanceConfiguration Config)
        : IHaveConfiguration<ProjectionInstanceConfiguration>
    {
        public IHaveConfiguration<ProjectionInstanceConfiguration> WithModifiedConfig(
            Func<ProjectionInstanceConfiguration, ProjectionInstanceConfiguration> modify)
        {
            return this with
            {
                Config = modify(Config)
            };
        }
    }
}