using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;
using Raven.Client.Documents;

namespace DC.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionStorageConfigurationSetup<string, TDocument> WithRavenDbDocumentStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup,
        IDocumentStore documentStore) where TDocument : notnull
    {
        var storage = new RavenDbProjectionStorage<TDocument>(documentStore);
        
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