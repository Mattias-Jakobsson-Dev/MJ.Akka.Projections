using System.Collections.Immutable;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.TestData;

public static class TestProjectionConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> WithTestProjection<TId>(
        this IHaveConfiguration<ProjectionSystemConfiguration<SetupInMemoryStorage>> setup,
        IImmutableList<object> initialEvents,
        IImmutableList<StorageFailures> failures,
        Func<IHaveConfiguration<ProjectionInstanceConfiguration>,
            IHaveConfiguration<ProjectionInstanceConfiguration>>? configure = null)
        where TId : notnull
    {
        return setup
            .WithProjection(
                new TestProjection<TId>(initialEvents, failures),
                x => configure?.Invoke(x) ?? x);
    }
}