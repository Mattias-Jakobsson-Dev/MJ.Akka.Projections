using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace DC.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IConfigurePart<TConfig, RavenDbProjectionStorage> WithRavenDbDocumentStorage<TConfig>(
        this IHaveConfiguration<TConfig> source,
        IDocumentStore documentStore,
        BulkInsertOptions? insertOptions = null) where TConfig : ProjectionConfig
    {
        return source.WithProjectionStorage(new RavenDbProjectionStorage(
            documentStore,
            insertOptions ?? new BulkInsertOptions
            {
                SkipOverwriteIfUnchanged = true
            }));
    }
    
    public static IConfigurePart<TConfig, RavenDbProjectionPositionStorage> WithRavenDbPositionStorage<TConfig>(
        this IHaveConfiguration<TConfig> source,
        IDocumentStore documentStore) where TConfig : ProjectionConfig
    {
        return source.WithPositionStorage(new RavenDbProjectionPositionStorage(documentStore));
    }
}