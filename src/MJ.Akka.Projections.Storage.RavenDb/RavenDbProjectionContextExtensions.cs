using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class RavenDbProjectionContextExtensions
{
    public static void WithTimeSeriesValue<TDocument>(
        this RavenDbProjectionContext<TDocument> source,
        string timeSeriesName,
        DateTime timestamp,
        double values,
        string? tag = null)
        where TDocument : class
    {
        source.WithTimeSeriesValues(
            timeSeriesName,
            timestamp,
            ImmutableList.Create(values),
            tag);
    }
}