using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace MJ.Akka.Projections.OpenTelemetry;

[PublicAPI]
public static class OpenTelemetryProjectionExtensions
{
    /// <summary>
    /// Enables OpenTelemetry instrumentation for the projection system.
    /// <para>
    /// This wraps position storage with latency histograms. Traces (spans) for
    /// per-document event handling and metrics for event throughput, position,
    /// queue depth, and restarts are emitted automatically once the projection
    /// pipeline is running.
    /// </para>
    /// <para>
    /// Register in your OTEL pipeline:
    /// <code>
    /// tracerProviderBuilder.AddSource(ProjectionInstrumentation.ActivitySourceName);
    /// meterProviderBuilder.AddMeter(ProjectionInstrumentation.MeterName);
    /// </code>
    /// </para>
    /// </summary>
    public static IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> WithOpenTelemetry<TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> config)
        where TStorageSetup : IStorageSetup
    {
        return config.WithModifiedStorage(new InstrumentedStorageModifier());
    }

    /// <summary>
    /// Registers the MJ.Akka.Projections <see cref="System.Diagnostics.ActivitySource"/>
    /// with the <see cref="TracerProviderBuilder"/> so that spans are exported.
    /// </summary>
    public static TracerProviderBuilder AddMJAkkaProjectionsInstrumentation(
        this TracerProviderBuilder builder)
    {
        return builder.AddSource(ProjectionInstrumentation.ActivitySourceName);
    }

    /// <summary>
    /// Registers the MJ.Akka.Projections <see cref="System.Diagnostics.Metrics.Meter"/>
    /// with the <see cref="MeterProviderBuilder"/> so that metrics are exported.
    /// </summary>
    public static MeterProviderBuilder AddMJAkkaProjectionsInstrumentation(
        this MeterProviderBuilder builder)
    {
        return builder.AddMeter(ProjectionInstrumentation.MeterName);
    }
}

