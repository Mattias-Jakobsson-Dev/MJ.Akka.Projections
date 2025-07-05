using JetBrains.Annotations;
using MJ.Akka.Projections.InProc;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

[PublicAPI]
public static class InProcProjectorFactoryConfigurationExtensions
{
    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, KeepTrackOfProjectorsInProc>
        WithInProcProjectionFactory<TStorageSetup>(
            this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> source)
        where TStorageSetup : IStorageSetup
    {
        return source.WithProjectionFactory(new KeepTrackOfProjectorsInProc(source.ActorSystem,
            new MaxNumberOfProjectorsPassivation(1_000)));
    }

    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, KeepTrackOfProjectorsInProc>
        KeepAllInMemory<TStorageSetup>(
            this IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, KeepTrackOfProjectorsInProc> source)
        where TStorageSetup : IStorageSetup
    {
        return source.WithProjectionFactory(new KeepTrackOfProjectorsInProc(
            source.ActorSystem,
            new KeepAllProjectors()));
    }

    public static IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, KeepTrackOfProjectorsInProc>
        KeepLimitedInMemory<TStorageSetup>(
            this IConfigurePart<ProjectionSystemConfiguration<TStorageSetup>, KeepTrackOfProjectorsInProc> source,
            int numberToKeepInMemory = 1_000)
        where TStorageSetup : IStorageSetup
    {
        return source.WithProjectionFactory(new KeepTrackOfProjectorsInProc(
            source.ActorSystem,
            new MaxNumberOfProjectorsPassivation(numberToKeepInMemory)));
    }
}