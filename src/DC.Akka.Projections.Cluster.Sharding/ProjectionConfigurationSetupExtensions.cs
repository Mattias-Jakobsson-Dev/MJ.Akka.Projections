using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Cluster.Sharding;

[PublicAPI]
public static class ProjectionConfigurationSetupExtensions
{
    public static IProjectionConfigurationSetup<TId, TDocument> WithSharding<TId, TDocument>(
        this IProjectionConfigurationSetup<TId, TDocument> setup,
        Func<string, TId> parseId,
        int maxNumberOfShards = 100,
        Func<ClusterShardingSettings, ClusterShardingSettings>? configureShard = null,
        Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
        where TId : notnull
        where TDocument : notnull
    {
        var coordinator = CreateCoordinator(setup, configureCoordinator);

        var projectionShard = ClusterSharding.Get(setup.ActorSystem).Start(
            typeName: $"projection-{setup.Projection.Name}",
            entityPropsFactory: id => Props.Create(() => new DocumentProjection<TId, TDocument>(
                setup.Projection.Name,
                parseId(id),
                null)),
            settings: (configureShard ?? (x => x))(ClusterShardingSettings.Create(setup.ActorSystem)),
            messageExtractor: new MessageExtractor<TId, TDocument>(maxNumberOfShards));

        return setup
            .WithCoordinatorFactory(() => Task.FromResult(coordinator))
            .WithProjectionFactory(_ => Task.FromResult(projectionShard));
    }

    public static IProjectionConfigurationSetup<TId, TDocument> AsClusterSingleton<TId, TDocument>(
        this IProjectionConfigurationSetup<TId, TDocument> setup,
        Func<string, TId> parseId,
        Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
        where TId : notnull
        where TDocument : notnull
    {
        var coordinator = CreateCoordinator(setup, configureCoordinator);

        return setup
            .WithCoordinatorFactory(() => Task.FromResult(coordinator));
    }

    private static IActorRef CreateCoordinator<TId, TDocument>(
        IProjectionConfigurationSetup<TId, TDocument> setup,
        Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
        where TId : notnull
        where TDocument : notnull
    {
        return setup.ActorSystem.ActorOf(ClusterSingletonManager.Props(
                singletonProps: Props.Create(() => new ProjectionsCoordinator<TId, TDocument>(setup.Projection.Name)),
                terminationMessage: PoisonPill.Instance,
                settings: (configureCoordinator ?? (x => x))(
                    ClusterSingletonManagerSettings.Create(setup.ActorSystem))),
            name: setup.Projection.Name);
    }

    private class MessageExtractor<TId, TDocument>(int maxNumberOfShards)
        : HashCodeMessageExtractor(maxNumberOfShards) where TId : notnull where TDocument : notnull
    {
        public override string EntityId(object message)
        {
            return (message as DocumentProjection<TId, TDocument>.Commands.ProjectEvents)?.Id.ToString() ?? "";
        }
    }
}