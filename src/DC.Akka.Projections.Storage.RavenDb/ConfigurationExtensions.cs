using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;
using Raven.Client.Documents;

namespace DC.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionsSetup WithRavenDbDocumentStorage(
        this IProjectionsSetup setup,
        IDocumentStore documentStore)
    {
        var storage = new RavenDbProjectionStorage(documentStore);
        
        return setup.WithProjectionStorage(storage);
    }
    
    public static IProjectionsSetup WithBatchedRavenDbDocumentStorage(
        this IProjectionsSetup setup,
        IDocumentStore documentStore,
        int batchSize = 100,
        int parallelism = 5)
    {
        var storage = new RavenDbProjectionStorage(documentStore)
            .Batched(
                setup.ActorSystem,
                batchSize, 
                parallelism);
        
        return setup.WithProjectionStorage(storage);
    }
    
    public static IProjectionsSetup WithRavenDbPositionStorage(
        this IProjectionsSetup setup,
        IDocumentStore documentStore)
    {
        var storage = new RavenDbProjectionPositionStorage(documentStore);
        
        return setup.WithPositionStorage(storage);
    }
}