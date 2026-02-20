using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public class RavenDbProjectionContext<TDocument>(
    string id,
    TDocument? document,
    IImmutableDictionary<string, object> metadata,
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> addedTimeSeries)
    : ContextWithDocument<string, TDocument>(id, document), IRavenDbProjectionContext
    where TDocument : class
{
    private IImmutableDictionary<string, object> _metadata = metadata;
    private IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> _addedTimeSeries = addedTimeSeries;

    public RavenDbProjectionContext(
        string id,
        TDocument? document,
        IImmutableDictionary<string, object> metadata) : this(
        id,
        document,
        metadata,
        ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty)
    {
    }

    public void SetMetadata(string key, object value)
    {
        _metadata = _metadata.SetItem(key, value);
    }

    public object? GetMetadata(string key)
    {
        return _metadata.GetValueOrDefault(key);
    }

    public override IProjectionContext Freeze()
    {
        var timeSeries = _addedTimeSeries;
        
        _addedTimeSeries = ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty;

        return new RavenDbProjectionContext<TDocument>(Id, Document, _metadata, timeSeries);
    }

    public override IProjectionContext MergeWith(IProjectionContext later)
    {
        if (later is RavenDbProjectionContext<TDocument> parsedLater)
        {
            var newMetadata = parsedLater
                ._metadata.Aggregate(
                    _metadata,
                    (current, item) => current
                        .SetItem(item.Key, item.Value));

            var newTimeSeries = parsedLater
                ._addedTimeSeries
                .AddRange(_addedTimeSeries);

            return new RavenDbProjectionContext<TDocument>(
                Id,
                parsedLater.Document,
                parsedLater.Document != null ? newMetadata : ImmutableDictionary<string, object>.Empty,
                parsedLater.Document != null ? newTimeSeries : ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty);
        }

        return later;
    }

    public void AddTimeSeries(TimeSeriesInput input)
    {
        _addedTimeSeries = _addedTimeSeries.SetItem(
            input.Name,
            _addedTimeSeries.TryGetValue(input.Name, out var current)
                ? current.Add(new TimeSeriesRecord(input.Timestamp, input.Values, input.Tag))
                : ImmutableList.Create(new TimeSeriesRecord(input.Timestamp, input.Values, input.Tag)));
    }

    object? IRavenDbProjectionContext.Document => Document;
    IImmutableDictionary<string, object> IRavenDbProjectionContext.Metadata => _metadata;
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> IRavenDbProjectionContext.AddedTimeSeries => _addedTimeSeries;
}

internal interface IRavenDbProjectionContext
{
    string Id { get; }
    object? Document { get; }
    IImmutableDictionary<string, object> Metadata { get; }
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> AddedTimeSeries { get; }
}