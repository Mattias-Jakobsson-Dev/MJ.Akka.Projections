using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public static class InMemoryStorageConfigurationExtensions
{
    public static IProjectionStoragePartSetup<T> WithInMemoryStorage<T>(
        this IProjectionPartSetup<T> setup) where T : IProjectionPartSetup<T>
    {
        return setup.WithProjectionStorage(new InMemoryProjectionStorage());
    }
    
    public static IProjectionConfigurationSetup<string, TDocument> WithInMemoryPositionStorage<TDocument>(
        this IProjectionConfigurationSetup<string, TDocument> setup) where TDocument : notnull
    {
        return setup.WithPositionStorage(new InMemoryPositionStorage());
    }
}