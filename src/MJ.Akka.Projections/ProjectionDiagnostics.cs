using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MJ.Akka.Projections;

/// <summary>
/// Shared <see cref="ActivitySource"/> and <see cref="Meter"/> used by the core
/// projection actors.  These are .NET BCL primitives — the OpenTelemetry SDK
/// is NOT required here; it is only needed in the optional
/// <c>MJ.Akka.Projections.OpenTelemetry</c> package.
/// </summary>
internal static class ProjectionDiagnostics
{
    internal const string ActivitySourceName = "MJ.Akka.Projections";
    internal const string MeterName = "MJ.Akka.Projections";

    internal static readonly ActivitySource ActivitySource =
        new(ActivitySourceName, "1.0.0");

    internal static readonly Meter Meter =
        new(MeterName, "1.0.0");

    // ── Counters ─────────────────────────────────────────────────────────────

    internal static readonly Counter<long> EventsProcessed =
        Meter.CreateCounter<long>(
            "projection.events.processed",
            unit: "{events}",
            description: "Total number of events successfully processed by a projection.");

    internal static readonly Counter<long> EventsFailed =
        Meter.CreateCounter<long>(
            "projection.events.failed",
            unit: "{events}",
            description: "Total number of events that failed processing in a projection.");

    internal static readonly Counter<long> RestartCount =
        Meter.CreateCounter<long>(
            "projection.restarts",
            unit: "{restarts}",
            description: "Number of times a projection source has been restarted.");

    // ── Observable Gauge ──────────────────────────────────────────────────────

    private static readonly Dictionary<string, long> LatestPositions = new();
    private static readonly object PositionLock = new();

    internal static void RecordPosition(string projectionName, long? position)
    {
        if (position == null) return;
        lock (PositionLock)
            LatestPositions[projectionName] = position.Value;
    }

    internal static readonly ObservableGauge<long> LatestPosition =
        Meter.CreateObservableGauge(
            "projection.position",
            observeValues: () =>
            {
                lock (PositionLock)
                {
                    return LatestPositions
                        .Select(kv => new Measurement<long>(
                            kv.Value,
                            new KeyValuePair<string, object?>("projection.name", kv.Key)))
                        .ToList();
                }
            },
            unit: "{events}",
            description: "Latest committed event position for each projection.");

    // ── Histograms ────────────────────────────────────────────────────────────

    internal static readonly Histogram<double> EventHandlingDuration =
        Meter.CreateHistogram<double>(
            "projection.event_handling.duration",
            unit: "ms",
            description: "Duration of a per-document event-handling cycle in milliseconds.");

    internal static readonly Histogram<double> PositionStorageLoadDuration =
        Meter.CreateHistogram<double>(
            "projection.position_storage.load.duration",
            unit: "ms",
            description: "Duration of loading the latest position from position storage.");

    internal static readonly Histogram<double> PositionStorageStoreDuration =
        Meter.CreateHistogram<double>(
            "projection.position_storage.store.duration",
            unit: "ms",
            description: "Duration of storing the latest position to position storage.");

    // ── UpDownCounters ────────────────────────────────────────────────────────

    internal static readonly UpDownCounter<long> ActiveGroups =
        Meter.CreateUpDownCounter<long>(
            "projection.groups.active",
            unit: "{groups}",
            description: "Number of in-flight projection groups in the sequencer.");

    internal static readonly UpDownCounter<long> ActiveTasks =
        Meter.CreateUpDownCounter<long>(
            "projection.tasks.active",
            unit: "{tasks}",
            description: "Number of in-flight projection tasks in the sequencer.");

    internal static readonly UpDownCounter<long> QueuedEvents =
        Meter.CreateUpDownCounter<long>(
            "projection.queue.depth",
            unit: "{batches}",
            description: "Number of event batches waiting in per-ID sequencer queues.");
}

