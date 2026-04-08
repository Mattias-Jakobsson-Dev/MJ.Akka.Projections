using JetBrains.Annotations;

namespace MJ.Akka.Projections.OpenTelemetry;

/// <summary>
/// Constants for the ActivitySource and Meter names emitted by MJ.Akka.Projections.
/// Use these when registering OTEL pipelines.
/// </summary>
[PublicAPI]
public static class ProjectionInstrumentation
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.ActivitySource"/> used by
    /// MJ.Akka.Projections. Pass to <c>tracerProviderBuilder.AddSource(...)</c>.
    /// </summary>
    public const string ActivitySourceName = "MJ.Akka.Projections";

    /// <summary>
    /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> used by
    /// MJ.Akka.Projections. Pass to <c>meterProviderBuilder.AddMeter(...)</c>.
    /// </summary>
    public const string MeterName = "MJ.Akka.Projections";
}
