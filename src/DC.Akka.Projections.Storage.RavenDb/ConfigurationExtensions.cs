using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace DC.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IProjectionStoragePartSetup<T> WithRavenDbDocumentStorage<T>(
        this IProjectionPartSetup<T> setup,
        IDocumentStore documentStore,
        BulkInsertOptions? insertOptions = null)
        where T : IProjectionPartSetup<T>
    {
        return setup.WithProjectionStorage(CreateStorage(documentStore, insertOptions));
    }
    
    public static IProjectionPartSetup<T> WithRavenDbPositionStorage<T>(
        this IProjectionPartSetup<T> setup,
        IDocumentStore documentStore)
        where T : IProjectionPartSetup<T>
    {
        var storage = new RavenDbProjectionPositionStorage(documentStore);

        return setup.WithPositionStorage(storage);
    }

    private static RavenDbProjectionStorage CreateStorage(
        IDocumentStore documentStore,
        BulkInsertOptions? insertOptions)
    {
        return new RavenDbProjectionStorage(
            documentStore,
            insertOptions ?? new BulkInsertOptions
            {
                SkipOverwriteIfUnchanged = true
            });
    }
}