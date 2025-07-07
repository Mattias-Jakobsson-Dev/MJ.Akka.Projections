using System.Collections.Immutable;
using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record RavenDbStorageProjectorResult(
    IImmutableDictionary<object, object> DocumentsToUpsert,
    IImmutableDictionary<string, IImmutableDictionary<string, object>> MetadataToUpsert,
    IImmutableList<object> DocumentsToDelete,
    IImmutableDictionary<string, IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>> TimeSeriesToAdd)
    : DocumentsStorageProjectorResult(DocumentsToUpsert, DocumentsToDelete)
{
    public static RavenDbStorageProjectorResult Empty =>
        new(ImmutableDictionary<object, object>.Empty,
            ImmutableDictionary<string, IImmutableDictionary<string, object>>.Empty, 
            ImmutableList<object>.Empty,
            ImmutableDictionary<string, IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>>.Empty);
    
    public RavenDbStorageProjectorResult WithMetadata(string id, string key, object value)
    {
        if (!MetadataToUpsert.TryGetValue(id, out var existingMetadata))
        {
            existingMetadata = ImmutableDictionary<string, object>.Empty;
        }

        existingMetadata = existingMetadata.SetItem(key, value);

        return this with
        {
            MetadataToUpsert = MetadataToUpsert.SetItem(id, existingMetadata)
        };
    }
    
    public RavenDbStorageProjectorResult WithTimeSeries(
        string id,
        string name,
        IImmutableList<TimeSeriesRecord> records)
    {
        var timeSeries = TimeSeriesToAdd;
        
        if (!timeSeries.TryGetValue(id, out var timeSeriesData))
        {
            timeSeriesData = ImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>.Empty;
        }

        if (!timeSeriesData.TryGetValue(name, out var existingRecords))
        {
            existingRecords = ImmutableList<TimeSeriesRecord>.Empty;
        }

        existingRecords = existingRecords.AddRange(records);
        
        timeSeries = timeSeries.SetItem(
            id,
            timeSeriesData.SetItem(name, existingRecords));

        return this with
        {
            TimeSeriesToAdd = timeSeries
        };
    }
}