using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public static class InMemoryStorageConfigurationExtensions
{
    public static IConfigurePart<TConfig, InMemoryProjectionStorage> WithInMemoryStorage<TConfig>(
        this IHaveConfiguration<TConfig> source) where TConfig : ContinuousProjectionConfig
    {
        return source.WithProjectionStorage(new InMemoryProjectionStorage());
    }
    
    public static IConfigurePart<TConfig, InMemoryPositionStorage> WithInMemoryPositionStorage<TConfig>(
        this IHaveConfiguration<TConfig> source) where TConfig : ContinuousProjectionConfig
    {
        return source.WithPositionStorage(new InMemoryPositionStorage());
    }
}