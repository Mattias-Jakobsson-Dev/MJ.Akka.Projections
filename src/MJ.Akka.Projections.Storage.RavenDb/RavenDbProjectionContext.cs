using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public class RavenDbProjectionContext<TDocument>(
    string id,
    TDocument? document,
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> timeSeries)
    : ProjectedDocumentContext<string, TDocument>(id, document)
    where TDocument : class
{
    private readonly bool _existed = document != null;
    private IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> _timeSeries = timeSeries;

    public RavenDbProjectionContext(string id, TDocument? document) 
        : this(id, document, ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty)
    {
        
    }
    
    internal IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> GetTimeSeries()
    {
        return _timeSeries;
    }
    
    public void WithTimeSeriesValues(
        string timeSeriesName,
        DateTime timestamp,
        IImmutableList<double> values,
        string? tag = null)
    {
        var currentValues = _timeSeries.TryGetValue(timeSeriesName, out var series)
            ? series
            : ImmutableList<TimeSeriesRecord>.Empty;

        _timeSeries = _timeSeries.SetItem(
            timeSeriesName,
            currentValues
                .Add(new TimeSeriesRecord(timestamp, values, tag)));
    }
    
    public override PrepareForStorageResponse PrepareForStorage()
    {
        var results = ImmutableList<ICanBePersisted>.Empty;

        if (Exists())
            results = results.Add(new PersistDocument(Id, Document));
        else if (_existed && !Exists())
            results = results.Add(new DeleteDocument(Id));

        foreach (var timeSeries in _timeSeries)
        {
            results = results.Add(new StoreTimeSeries(Id, timeSeries.Key, timeSeries.Value));
        }

        return new PrepareForStorageResponse(
            Id,
            results,
            new RavenDbProjectionContext<TDocument>(Id, Document));
    }
}