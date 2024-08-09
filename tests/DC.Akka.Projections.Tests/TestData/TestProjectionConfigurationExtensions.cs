using System.Collections.Immutable;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections.Tests.TestData;

public static class TestProjectionConfigurationExtensions
{
    public static IProjectionsSetup WithTestProjection<TId>(
        this IProjectionsSetup setup,
        IImmutableList<object> initialEvents,
        Func<IProjectionConfigurationSetup<TId, TestDocument<TId>>,
            IProjectionConfigurationSetup<TId, TestDocument<TId>>>? configure = null)
        where TId : notnull
    {
        return setup
            .WithProjection(
                new TestProjection<TId>(initialEvents),
                x => configure?.Invoke(x) ?? x);
    }
}