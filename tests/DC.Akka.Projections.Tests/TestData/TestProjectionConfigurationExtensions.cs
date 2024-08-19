using System.Collections.Immutable;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Tests.TestData;

public static class TestProjectionConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration> WithTestProjection<TId>(
        this IHaveConfiguration<ProjectionSystemConfiguration> setup,
        IImmutableList<object> initialEvents,
        Func<IHaveConfiguration<ProjectionInstanceConfiguration>,
            IHaveConfiguration<ProjectionInstanceConfiguration>>? configure = null)
        where TId : notnull
    {
        return setup
            .WithProjection(
                new TestProjection<TId>(initialEvents),
                x => configure?.Invoke(x) ?? x);
    }
}