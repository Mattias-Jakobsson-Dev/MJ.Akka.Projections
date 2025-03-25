using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public record DocumentWithTimeSeries<TDocument>(
    TDocument Document,
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> TimeSeries)
    : IResetDocument<DocumentWithTimeSeries<TDocument>>, IHaveTimeSeries
    where TDocument : notnull
{
    public static DocumentWithTimeSeries<TDocument> FromDocument(TDocument document)
    {
        return new DocumentWithTimeSeries<TDocument>(
            document,
            ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty);
    }

    public DocumentWithTimeSeries<TDocument> ModifyDocument(Func<TDocument, TDocument> modification)
    {
        return this with
        {
            Document = modification(Document)
        };
    }

    public DocumentWithTimeSeries<TDocument> WithTimeSeriesValues(
        string timeSeriesName,
        DateTime timestamp,
        IImmutableList<double> values,
        string? tag = null)
    {
        var currentValues = TimeSeries.TryGetValue(timeSeriesName, out var series)
            ? series
            : ImmutableList<TimeSeriesRecord>.Empty;

        return this with
        {
            TimeSeries = TimeSeries.SetItem(
                timeSeriesName,
                currentValues
                    .Add(new TimeSeriesRecord(timestamp, values, tag)))
        };
    }

    public DocumentWithTimeSeries<TDocument> Reset()
    {
        return new DocumentWithTimeSeries<TDocument>(
            Document is IResetDocument<TDocument> resetDocument ? resetDocument.Reset() : Document,
            TimeSeries.Clear());
    }

    public object GetDocument()
    {
        return Document;
    }
}