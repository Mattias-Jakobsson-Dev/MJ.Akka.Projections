using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonPositionStorage : IProjectionPositionStorage
{
    private readonly IActorRef _storage;

    private ClusterSingletonPositionStorage(IActorRef storage)
    {
        _storage = storage;
    }

    public async Task<long?> LoadLatestPosition(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var response = await _storage.Ask<Storage.Responses.LoadPositionResponse>(
            new Storage.Queries.LoadPosition(projectionName), 
            cancellationToken: cancellationToken);

        if (response.Error != null)
            throw response.Error;

        return response.Position;
    }

    public async Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var response = await _storage.Ask<Storage.Responses.StorePositionResponse>(
            new Storage.Commands.StorePosition(projectionName, position),
            cancellationToken: cancellationToken);

        if (response.Error != null)
            throw response.Error;

        return response.Position;
    }

    public static ClusterSingletonPositionStorage Create(
        ActorSystem actorSystem,
        string name,
        ClusterSingletonManagerSettings settings)
    {
        var coordinatorSettings = settings
            .WithSingletonName(name);
                
        actorSystem
            .ActorOf(ClusterSingletonManager.Props(
                    Props.Create(() => new Storage()),
                    coordinatorSettings),
                name);

        var storage = actorSystem
            .ActorOf(ClusterSingletonProxy.Props(
                    $"/user/{name}",
                    ClusterSingletonProxySettings
                        .Create(actorSystem)
                        .WithRole(coordinatorSettings.Role)
                        .WithSingletonName(coordinatorSettings.SingletonName)),
                $"{name}-proxy");

        return new ClusterSingletonPositionStorage(storage);
    }
    
    private class Storage : ReceiveActor
    {
        public static class Commands
        {
            public record StorePosition(string ProjectionName, long? Position);
        }
        
        public static class Queries
        {
            public record LoadPosition(string ProjectionName);
        }
        
        public static class Responses
        {
            public record StorePositionResponse(long? Position, Exception? Error = null);

            public record LoadPositionResponse(long? Position, Exception? Error = null);
        }

        public Storage()
        {
            var storage = new InMemoryPositionStorage();

            ReceiveAsync<Commands.StorePosition>(async cmd =>
            {
                try
                {
                    var position = await storage.StoreLatestPosition(cmd.ProjectionName, cmd.Position);
                    
                    Sender.Tell(new Responses.StorePositionResponse(position));
                }
                catch (Exception e)
                {
                    Sender.Tell(new Responses.StorePositionResponse(null, e));
                }
            });
            
            ReceiveAsync<Queries.LoadPosition>(async query =>
            {
                try
                {
                    var position = await storage.LoadLatestPosition(query.ProjectionName);

                    Sender.Tell(new Responses.LoadPositionResponse(position));
                }
                catch (Exception e)
                {
                    Sender.Tell(new Responses.LoadPositionResponse(null, e));
                }
            });
        }
    }
}