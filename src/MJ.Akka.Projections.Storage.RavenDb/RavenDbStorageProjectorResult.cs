using System.Collections.Immutable;
using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record RavenDbStorageProjectorResult(
    IImmutableDictionary<object, object> DocumentsToUpsert,
    IImmutableList<object> DocumentsToDelete,
    IImmutableDictionary<string, IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>> TimeSeriesToAdd)
    : DocumentsStorageProjectorResult(DocumentsToUpsert, DocumentsToDelete)
{
    public static RavenDbStorageProjectorResult Empty =>
        new(ImmutableDictionary<object, object>.Empty,
            ImmutableList<object>.Empty,
            ImmutableDictionary<string, IImmutableDictionary<string, IImmutableList<TimeSeriesRecord>>>.Empty);
    
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