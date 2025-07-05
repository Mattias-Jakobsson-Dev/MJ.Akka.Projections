using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ConfigurationExtensions
{
    public static IConfigurePart<TConfig, RavenDbProjectionPositionStorage> WithRavenDbPositionStorage<TConfig>(
        this IHaveConfiguration<TConfig> source,
        IDocumentStore documentStore) where TConfig : ContinuousProjectionConfig
    {
        return source.WithPositionStorage(new RavenDbProjectionPositionStorage(documentStore));
    }
}