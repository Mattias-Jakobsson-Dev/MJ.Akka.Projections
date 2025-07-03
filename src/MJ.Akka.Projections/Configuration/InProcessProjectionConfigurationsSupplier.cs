namespace MJ.Akka.Projections.Configuration;

public class InProcessProjectionConfigurationsSupplier(ProjectionConfiguration configuration)
    : ISupplyProjectionConfigurations
{
    public ProjectionConfiguration GetConfiguration()
    {
        return configuration;
    }
}