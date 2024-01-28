using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Cluster.Sharding;

public static class ProjectionConfigurationSetupExtensions
{
    public static IProjectionConfigurationSetup<TDocument> WithSharding<TDocument>(
        this IProjectionConfigurationSetup<TDocument> setup,
        int maxNumberOfShards = 100,
        Func<ClusterShardingSettings, ClusterShardingSettings>? configureShard = null,
        Func<ClusterSingletonManagerSettings, ClusterSingletonManagerSettings>? configureCoordinator = null)
    {
        var coordinator = setup.System.ActorOf(ClusterSingletonManager.Props(
                singletonProps: Props.Create(() => new ProjectionsCoordinator<TDocument>(setup.Projection.Name)),
                terminationMessage: PoisonPill.Instance,
                settings: (configureCoordinator ?? (x => x))(ClusterSingletonManagerSettings.Create(setup.System))),
            name: setup.Projection.Name);
        
        var projectionShard = ClusterSharding.Get(setup.System).Start(
            typeName: $"projection-{setup.Projection.Name}",
            entityPropsFactory: id => Props.Create(() => new DocumentProjection<TDocument>(
                setup.Projection.Name,
                id)),
            settings: (configureShard ?? (x => x))(ClusterShardingSettings.Create(setup.System)),
            messageExtractor: new MessageExtractor<TDocument>(maxNumberOfShards));
        
        return setup
            .AutoStart()
            .WithCoordinatorFactory(() => Task.FromResult(coordinator))
            .WithProjectionFactory(_ => Task.FromResult(projectionShard));
    }
    
    private class MessageExtractor<TDocument>(int maxNumberOfShards) : HashCodeMessageExtractor(maxNumberOfShards)
    {
        public override string EntityId(object message)
        {
            return (message as DocumentProjection<TDocument>.Commands.ProjectEvents)?.Id.ToString() ?? "";
        }
    }
}