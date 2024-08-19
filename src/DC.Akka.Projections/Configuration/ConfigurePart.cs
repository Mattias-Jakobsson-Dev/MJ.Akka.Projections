using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

internal class ConfigurePart<TConfig, TPart>(IHaveConfiguration<TConfig> baseConfig, TPart itemUnderConfiguration)
    : IConfigurePart<TConfig, TPart>
    where TConfig : ProjectionConfig
{
    public TPart ItemUnderConfiguration { get; } = itemUnderConfiguration;
    public ActorSystem ActorSystem => baseConfig.ActorSystem;
    public TConfig Config => baseConfig.Config;

    public IHaveConfiguration<TConfig> WithModifiedConfig(Func<TConfig, TConfig> modify)
    {
        return baseConfig.WithModifiedConfig(modify);
    }
}