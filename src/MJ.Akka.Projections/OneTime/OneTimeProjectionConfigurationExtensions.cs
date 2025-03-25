using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.OneTime;

public static class OneTimeProjectionConfigurationExtensions
{
    public static IHaveConfiguration<OneTimeProjectionConfig> StartFrom(
        this IHaveConfiguration<OneTimeProjectionConfig> setup,
        long? position)
    {
        return setup.WithModifiedConfig(conf => conf with
        {
            StartPosition = position
        });
    }
}