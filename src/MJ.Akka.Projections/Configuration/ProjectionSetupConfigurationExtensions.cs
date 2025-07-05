using Akka.Actor;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public static class ProjectionSetupConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration> WithProjection<TId, TContext>(
        this IHaveConfiguration<ProjectionSystemConfiguration> source,
        IProjection<TId, TContext> projection,
        Func<IHaveConfiguration<ProjectionInstanceConfiguration>, IHaveConfiguration<ProjectionInstanceConfiguration>>?
            configure = null)
        where TId : notnull where TContext : IProjectionContext<TId>
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

                        return new ProjectionConfiguration<TId, TContext>(
                            projection,
                            loadStorage,
                            configuredProjection.PositionStorage!,
                            conf.ProjectorFactory,
                            configuredProjection.RestartSettings,
                            configuredProjection.EventBatchingStrategy!,
                            configuredProjection.PositionBatchingStrategy!,
                            projection.Configure(new SetupProjection<TId, TContext>()).Build());
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