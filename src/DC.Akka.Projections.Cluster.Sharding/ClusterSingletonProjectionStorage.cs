using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Cluster.Sharding;

public class ClusterSingletonProjectionStorage : IProjectionStorage
{
    private readonly IActorRef _storage;

    private ClusterSingletonProjectionStorage(IActorRef storage)
    {
        _storage = storage;
    }

    public async Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default)
    {
        var response = await _storage.Ask<Storage.Responses.LoadDocumentResponse>(
            new Storage.Queries.LoadDocument(id),
            cancellationToken: cancellationToken);

        if (response.Error != null)
            throw response.Error;

        return (TDocument?)response.Document;
    }

    public async Task Store(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        CancellationToken cancellationToken = default)
    {
        var response = await _storage.Ask<Storage.Responses.StoreDocumentsResponse>(
            new Storage.Commands.StoreDocuments(toUpsert, toDelete), 
            cancellationToken: cancellationToken);

        if (response.Error != null)
            throw response.Error;
    }

    public static ClusterSingletonProjectionStorage Create(
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

        return new ClusterSingletonProjectionStorage(storage);
    }
    
    private class Storage : ReceiveActor
    {
        public static class Commands
        {
            public record StoreDocuments(
                IImmutableList<DocumentToStore> ToUpsert,
                IImmutableList<DocumentToDelete> ToDelete);
        }
        
        public static class Queries
        {
            public record LoadDocument(object Id);
        }
        
        public static class Responses
        {
            public record StoreDocumentsResponse(Exception? Error = null);

            public record LoadDocumentResponse(object? Document, Exception? Error = null);
        }

        public Storage()
        {
            var storage = new InMemoryProjectionStorage();

            ReceiveAsync<Commands.StoreDocuments>(async cmd =>
            {
                try
                {
                    await storage.Store(cmd.ToUpsert, cmd.ToDelete);
                    
                    Sender.Tell(new Responses.StoreDocumentsResponse());
                }
                catch (Exception e)
                {
                    Sender.Tell(new Responses.StoreDocumentsResponse(e));
                }
            });
            
            ReceiveAsync<Queries.LoadDocument>(async query =>
            {
                try
                {
                    var document = await storage.LoadDocument<object>(query.Id);

                    Sender.Tell(new Responses.LoadDocumentResponse(document));
                }
                catch (Exception e)
                {
                    Sender.Tell(new Responses.LoadDocumentResponse(null, e));
                }
            });
        }
    }
}