using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public static class InMemoryStorageConfigurationExtensions
{
    public static IProjectionStorageConfigurationSetup<string, TDocument> WithInMemoryStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup) where TDocument : notnull
    {
        return setup.WithProjectionStorage(new InMemoryProjectionStorage<string, TDocument>());
    }
    
    public static IProjectionConfigurationSetup<string, TDocument> WithInMemoryPositionStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup) where TDocument : notnull
    {
        return setup.WithPositionStorage(new InMemoryProjectionPositionStorage());
    }
}