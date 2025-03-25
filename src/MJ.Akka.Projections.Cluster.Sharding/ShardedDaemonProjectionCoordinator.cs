using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using MJ.Akka.Projections;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Cluster.Sharding;

public class ShardedDaemonProjectionCoordinator(
    IImmutableDictionary<string, IProjectionProxy> projections) : IProjectionsCoordinator
{
    public IProjectionProxy? Get(string projectionName)
    {
        return projections.GetValueOrDefault(projectionName);
    }

    public class Setup(
        ActorSystem actorSystem,
        string name,
        ShardedDaemonProcessSettings settings) : IConfigureProjectionCoordinator
    {
        private readonly Dictionary<string, IProjection> _projections = new();

        public void WithProjection(IProjection projection)
        {
            _projections[projection.Name] = projection;
        }

        public async Task<IProjectionsCoordinator> Start()
        {
            var sortedProjections = _projections
                .Select(x => x.Value)
                .OrderBy(x => x.Name)
                .ToImmutableList();

            ShardedDaemonProcess
                .Get(actorSystem)
                .Init(
                    name,
                    sortedProjections.Count,
                    id => sortedProjections[id].CreateCoordinatorProps(),
                    settings,
                    new ProjectionsCoordinator.Commands.Kill());

            var shardingRef = await ClusterSharding
                .Get(actorSystem)
                .StartProxyAsync(
                    $"sharded-daemon-process-{name}",
                    settings.Role ?? "",
                    new MessageExtractor(sortedProjections.Count));

            var projectionProxies = sortedProjections
                .Select((projection, index) => new
                {
                    Id = index,
                    Projection = projection
                })
                .ToImmutableDictionary(
                    x => x.Projection.Name, 
                    x => (IProjectionProxy)new ShardedDaemonProjectionProxy(
                        x.Projection,
                        x.Id,
                        shardingRef));

            return new ShardedDaemonProjectionCoordinator(projectionProxies);
        }
    }
    
    private class ShardedDaemonProjectionProxy(IProjection projection, int daemonId, IActorRef coordinator) 
        : IProjectionProxy
    {
        public IProjection Projection { get; } = projection;
        
        public Task Stop()
        {
            return coordinator.Ask<ProjectionsCoordinator.Responses.StopResponse>(
                WrapMessage(new ProjectionsCoordinator.Commands.Stop()));
        }

        public async Task WaitForCompletion(TimeSpan? timeout = null)
        {
            var response = await coordinator.Ask<ProjectionsCoordinator.Responses.WaitForCompletionResponse>(
                WrapMessage(new ProjectionsCoordinator.Commands.WaitForCompletion()),
                timeout ?? Timeout.InfiniteTimeSpan);

            if (response.Error != null)
                throw response.Error;
        }

        private ShardingEnvelope WrapMessage(object message)
        {
            return new ShardingEnvelope(daemonId.ToString(), message);
        }
    }

    private sealed class MessageExtractor(int maxNumberOfShards) : HashCodeMessageExtractor(maxNumberOfShards)
    {
        public override string EntityId(object message) => (message as ShardingEnvelope)?.EntityId ?? "";

        public override object? EntityMessage(object message) => (message as ShardingEnvelope)?.Message;

        public override string ShardId(string entityId, object? messageHint = null)
        {
            return entityId;
        }
    }
}