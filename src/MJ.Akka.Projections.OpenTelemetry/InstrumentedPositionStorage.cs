using System.Diagnostics;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.OpenTelemetry;

/// <summary>
/// Decorator for <see cref="IProjectionPositionStorage"/> that records
/// latency histograms for every load and store operation.
/// </summary>
internal sealed class InstrumentedPositionStorage(
    IProjectionPositionStorage inner) : IProjectionPositionStorage
{
    public async Task<long?> LoadLatestPosition(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await inner.LoadLatestPosition(projectionName, cancellationToken);
        }
        finally
        {
            sw.Stop();
            ProjectionDiagnostics.PositionStorageLoadDuration.Record(
                sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("projection.name", projectionName));
        }
    }

    public async Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await inner.StoreLatestPosition(projectionName, position, cancellationToken);

            ProjectionDiagnostics.RecordPosition(projectionName, result);

            return result;
        }
        finally
        {
            sw.Stop();
            ProjectionDiagnostics.PositionStorageStoreDuration.Record(
                sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("projection.name", projectionName));
        }
    }

    public Task Reset(
        string projectionName,
        long? position = null,
        CancellationToken cancellationToken = default)
    {
        return inner.Reset(projectionName, position, cancellationToken);
    }
}
