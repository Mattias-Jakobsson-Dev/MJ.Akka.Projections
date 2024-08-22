using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public static class ProjectionSetupConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration> WithProjection<TId, TDocument>(
        this IHaveConfiguration<ProjectionSystemConfiguration> source,
        IProjection<TId, TDocument> projection,
        Func<IHaveConfiguration<ProjectionInstanceConfiguration>, IHaveConfiguration<ProjectionInstanceConfiguration>>?
            configure = null)
        where TId : notnull where TDocument : notnull
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

                        return new ProjectionConfiguration<TId, TDocument>(
                            projection,
                            configuredProjection.ProjectionStorage!,
                            configuredProjection.PositionStorage!,
                            conf.ProjectorFactory,
                            configuredProjection.RestartSettings,
                            configuredProjection.EventBatchingStrategy!,
                            configuredProjection.PositionBatchingStrategy!,
                            projection.Configure(new SetupProjection<TId, TDocument>()).Build());
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