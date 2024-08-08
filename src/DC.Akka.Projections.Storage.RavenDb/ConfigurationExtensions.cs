using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;
using Raven.Client.Documents;

namespace DC.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionConfigurationSetup<string, TDocument> WithRavenDbDocumentStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup,
        IDocumentStore documentStore) where TDocument : notnull
    {
        var storage = new RavenDbProjectionStorage(documentStore);
        
        return setup.WithProjectionStorage(storage);
    }
    
    public static IProjectionConfigurationSetup<string, TDocument> WithBatchedRavenDbDocumentStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup,
        IDocumentStore documentStore,
        int batchSize = 100,
        int parallelism = 5) where TDocument : notnull
    {
        var storage = new RavenDbProjectionStorage(documentStore)
            .Batched(
                setup.ActorSystem,
                batchSize, 
                parallelism);
        
        return setup.WithProjectionStorage(storage);
    }
    
    public static IProjectionConfigurationSetup<string, TDocument> WithRavenDbPositionStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup,
        IDocumentStore documentStore) where TDocument : notnull
    {
        var storage = new RavenDbProjectionPositionStorage(documentStore);
        
        return setup.WithPositionStorage(storage);
    }
}