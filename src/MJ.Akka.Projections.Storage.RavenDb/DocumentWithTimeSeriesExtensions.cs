using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class DocumentWithTimeSeriesExtensions
{
    public static async Task<DocumentWithTimeSeries<TDocument>> ModifyDocument<TDocument>(
        this DocumentWithTimeSeries<TDocument> source,
        Func<TDocument, Task<TDocument>> modification)
        where TDocument : notnull
    {
        var modifiedDocument = await modification(source.Document);

        return source.ModifyDocument(_ => modifiedDocument);
    }

    public static DocumentWithTimeSeries<TDocument> WithTimeSeriesValue<TDocument>(
        this DocumentWithTimeSeries<TDocument> source,
        string timeSeriesName,
        DateTime timestamp,
        double values,
        string? tag = null)
        where TDocument : notnull
    {
        return source.WithTimeSeriesValues(
            timeSeriesName,
            timestamp,
            ImmutableList.Create(values),
            tag);
    }
}