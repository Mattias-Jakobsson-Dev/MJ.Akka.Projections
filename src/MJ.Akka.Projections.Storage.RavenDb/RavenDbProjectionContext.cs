using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public class RavenDbProjectionContext<TDocument>(
    SimpleIdContext<string> id,
    TDocument? document,
    IImmutableDictionary<string, object> metadata,
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> addedTimeSeries)
    : ContextWithDocument<SimpleIdContext<string>, TDocument>(id, document), IRavenDbProjectionContext
    where TDocument : class
{
    protected IImmutableDictionary<string, object> Metadata = metadata;
    protected IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> AddedTimeSeries = addedTimeSeries;

    public RavenDbProjectionContext(
        SimpleIdContext<string> id,
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
        Metadata = Metadata.SetItem(key, value);
    }

    public object? GetMetadata(string key)
    {
        return Metadata.GetValueOrDefault(key);
    }

    public override IProjectionContext Freeze()
    {
        return new RavenDbProjectionContext<TDocument>(Id, Document, Metadata, AddedTimeSeries);
    }

    public virtual IProjectionContext Reset()
    {
        return new RavenDbProjectionContext<TDocument>(
            Id,
            Document,
            Metadata);
    }

    public override IProjectionContext MergeWith(IProjectionContext later)
    {
        if (later is RavenDbProjectionContext<TDocument> parsedLater)
        {
            var newMetadata = parsedLater
                .Metadata.Aggregate(
                    Metadata,
                    (current, item) => current
                        .SetItem(item.Key, item.Value));

            var newTimeSeries = parsedLater
                .AddedTimeSeries
                .AddRange(AddedTimeSeries);

            return new RavenDbProjectionContext<TDocument>(
                Id,
                parsedLater.Document,
                parsedLater.Document != null ? newMetadata : ImmutableDictionary<string, object>.Empty,
                parsedLater.Document != null
                    ? newTimeSeries
                    : ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty);
        }

        return later;
    }

    public void AddTimeSeries(TimeSeriesInput input)
    {
        AddedTimeSeries = AddedTimeSeries.SetItem(
            input.Name,
            AddedTimeSeries.TryGetValue(input.Name, out var current)
                ? current.Add(new TimeSeriesRecord(input.Timestamp, input.Values, input.Tag))
                : ImmutableList.Create(new TimeSeriesRecord(input.Timestamp, input.Values, input.Tag)));
    }

    object? IRavenDbProjectionContext.Document => Document;
    IImmutableDictionary<string, object> IRavenDbProjectionContext.Metadata => Metadata;

    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> IRavenDbProjectionContext.AddedTimeSeries =>
        AddedTimeSeries;

    public string GetDocumentId()
    {
        return Id.GetStringRepresentation();
    }
}

internal interface IRavenDbProjectionContext : IResettableProjectionContext
{
    object? Document { get; }
    IImmutableDictionary<string, object> Metadata { get; }
    IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>> AddedTimeSeries { get; }

    string GetDocumentId();
}