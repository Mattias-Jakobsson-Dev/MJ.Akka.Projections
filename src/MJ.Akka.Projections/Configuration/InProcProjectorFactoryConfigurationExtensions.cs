using JetBrains.Annotations;
using MJ.Akka.Projections.InProc;

namespace MJ.Akka.Projections.Configuration;

[PublicAPI]
public static class InProcProjectorFactoryConfigurationExtensions
{
    public static IConfigurePart<ProjectionSystemConfiguration, KeepTrackOfProjectorsInProc>
        WithInProcProjectionFactory(this IHaveConfiguration<ProjectionSystemConfiguration> source)
    {
        return source.WithProjectionFactory(new KeepTrackOfProjectorsInProc(source.ActorSystem,
            new MaxNumberOfProjectorsPassivation(1_000)));
    }
    
    public static IConfigurePart<ProjectionSystemConfiguration, KeepTrackOfProjectorsInProc> KeepAllInMemory(
        this IConfigurePart<ProjectionSystemConfiguration, KeepTrackOfProjectorsInProc> source)
    {
        return source.WithProjectionFactory(new KeepTrackOfProjectorsInProc(
            source.ActorSystem,
            new KeepAllProjectors()));
    }
    
    public static IConfigurePart<ProjectionSystemConfiguration, KeepTrackOfProjectorsInProc> KeepLimitedInMemory(
        this IConfigurePart<ProjectionSystemConfiguration, KeepTrackOfProjectorsInProc> source,
        int numberToKeepInMemory = 1_000)
    {
        return source.WithProjectionFactory(new KeepTrackOfProjectorsInProc(
            source.ActorSystem,
            new MaxNumberOfProjectorsPassivation(numberToKeepInMemory)));
    }
}