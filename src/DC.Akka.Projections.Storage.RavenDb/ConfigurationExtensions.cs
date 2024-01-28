using DC.Akka.Projections.Configuration;
using Raven.Client.Documents;

namespace DC.Akka.Projections.Storage.RavenDb;

public static class ConfigurationExtensions
{
    public static IProjectionStorageConfigurationSetup<TDocument> WithRavenDbStorage<TDocument>(
        this IProjectionConfigurationSetup<TDocument> setup,
        IDocumentStore documentStore)
    {
        var storage = new RavenDbProjectionStorage(documentStore);
        
        return setup.WithStorage(storage);
    }
}