using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Akka.Streams;
using Akka.Streams.Dsl;
using AutoFixture;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.OpenTelemetry;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Shouldly;
using Xunit;

namespace MJ.Akka.Projections.Tests.OpenTelemetryTests;

/// <summary>
/// Verifies that the projection pipeline emits the expected OpenTelemetry metrics and traces.
/// Each test creates a fresh actor system and a scoped <see cref="MeterListener"/> so that only
/// measurements produced by that specific test run are captured.
/// </summary>
public class OtelProjectionTests(NormalTestKitActorSystem actorSystemHandler)
    : IClassFixture<NormalTestKitActorSystem>
{
    private readonly Fixture _fixture = new();
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    // ─── Event types used only inside these tests ────────────────────────────

    private record SimpleOtelEvent(string DocId, string EventId);
    private record FailingHandlerOtelEvent(string DocId);
    private record WithDataOtelEvent(string DocId, string EventId);

    // ─── Counter tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task Events_processed_counter_increments_for_each_handled_event()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new SimpleOtelProjection([
            new SimpleOtelEvent(docId, _fixture.Create<string>()),
            new SimpleOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        var processed = measurements
            .Where(m => m.InstrumentName == "projection.events.processed")
            .Sum(m => (long)m.Value);

        processed.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Events_failed_counter_increments_when_handler_throws()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new FailingHandlerOtelProjection([
            new FailingHandlerOtelEvent(docId)
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        // No restart settings → projection fails after the first handler exception
        await Should.ThrowAsync<Exception>(() =>
            coordinator.Get(projection.Name)!.WaitForCompletion(_timeout));

        var failed = measurements
            .Where(m => m.InstrumentName == "projection.events.failed")
            .Sum(m => (long)m.Value);

        failed.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Restart_counter_increments_on_each_stream_start()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new SimpleOtelProjection([
            new SimpleOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        // The restart source always fires once on initial start
        var restarts = measurements
            .Where(m => m.InstrumentName == "projection.restarts")
            .Sum(m => (long)m.Value);

        restarts.ShouldBeGreaterThanOrEqualTo(1);
    }

    // ─── Histogram tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Event_handling_duration_histogram_is_recorded()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new SimpleOtelProjection([
            new SimpleOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        measurements.ShouldContain(m =>
            m.InstrumentName == "projection.event_handling.duration" && m.Value >= 0);
    }

    // ─── WithData metrics ────────────────────────────────────────────────────

    [Fact]
    public async Task WithData_fetch_duration_histogram_is_recorded()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new WithDataOtelProjection([
            new WithDataOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        measurements.ShouldContain(m =>
            m.InstrumentName == "projection.withdata.fetch.duration" &&
            m.Tags.ContainsKey("event.type") &&
            (string?)m.Tags["event.type"] == nameof(WithDataOtelEvent));
    }

    [Fact]
    public async Task WithData_fetch_failure_counter_increments_when_getData_throws()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new FailingGetDataOtelProjection([
            new WithDataOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        // getData always throws → stream fails without restart
        await Should.ThrowAsync<Exception>(() =>
            coordinator.Get(projection.Name)!.WaitForCompletion(_timeout));

        var failures = measurements
            .Where(m => m.InstrumentName == "projection.withdata.fetch.failures")
            .Sum(m => (long)m.Value);

        failures.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task WithData_fetch_failure_counter_increments_then_succeeds_on_restart()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        // Fails on first getData call, succeeds on subsequent attempts
        var projection = new OnceFailingGetDataOtelProjection([
            new WithDataOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithRestartSettings(RestartSettings.Create(
                    TimeSpan.Zero, TimeSpan.Zero, 1).WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        var failures = measurements
            .Where(m => m.InstrumentName == "projection.withdata.fetch.failures")
            .Sum(m => (long)m.Value);

        failures.ShouldBe(1);
    }

    // ─── WithOpenTelemetry() position-storage metrics ────────────────────────

    [Fact]
    public async Task WithOpenTelemetry_records_position_storage_load_and_store_duration()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projection = new SimpleOtelProjection(
            [new SimpleOtelEvent(docId, _fixture.Create<string>())],
            name: "OtelPosTest_" + _fixture.Create<string>());

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithOpenTelemetry(),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        measurements.ShouldContain(m => m.InstrumentName == "projection.position_storage.load.duration");
        measurements.ShouldContain(m => m.InstrumentName == "projection.position_storage.store.duration");
    }

    [Fact]
    public async Task WithOpenTelemetry_updates_position_observable_gauge()
    {
        using var system = actorSystemHandler.StartNewActorSystem();
        var (listener, measurements) = StartCapturing();
        using var _ = listener;

        var docId = _fixture.Create<string>();
        var projectionName = "OtelGaugeTest_" + _fixture.Create<string>();
        var projection = new SimpleOtelProjection(
            [new SimpleOtelEvent(docId, _fixture.Create<string>())],
            name: projectionName);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithOpenTelemetry(),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        // Pull the observable gauge values
        listener.RecordObservableInstruments();

        var positionValues = measurements
            .Where(m =>
                m.InstrumentName == "projection.position" &&
                m.Tags.ContainsKey("projection.name") &&
                (string?)m.Tags["projection.name"] == projectionName)
            .ToList();

        positionValues.ShouldNotBeEmpty();
        positionValues.Max(m => m.Value).ShouldBeGreaterThanOrEqualTo(1);
    }

    // ─── Trace tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Span_is_emitted_per_document_handling_batch()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var activities = new ConcurrentBag<Activity>();
        var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ProjectionInstrumentation.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        using var __ = activityListener;
        ActivitySource.AddActivityListener(activityListener);

        var docId = _fixture.Create<string>();
        var projection = new SimpleOtelProjection([
            new SimpleOtelEvent(docId, _fixture.Create<string>()),
            new SimpleOtelEvent(docId, _fixture.Create<string>())
        ]);

        var coordinator = await system
            .Projections(config => config
                .WithProjection(projection)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        activities.ShouldNotBeEmpty();
        activities.ShouldContain(a => a.DisplayName.Contains(projection.Name));
    }

    // ─── MeterListener factory ────────────────────────────────────────────────

    private sealed record CapturedMeasurement(
        string InstrumentName,
        double Value,
        Dictionary<string, object?> Tags);

    private static (MeterListener Listener, ConcurrentBag<CapturedMeasurement> Measurements)
        StartCapturing()
    {
        var bag = new ConcurrentBag<CapturedMeasurement>();
        var meterListener = new MeterListener();

        meterListener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProjectionInstrumentation.MeterName)
                l.EnableMeasurementEvents(instrument);
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var tagDict = tags.ToArray().ToDictionary(t => t.Key, t => t.Value);
            bag.Add(new CapturedMeasurement(instrument.Name, value, tagDict));
        });

        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            var tagDict = tags.ToArray().ToDictionary(t => t.Key, t => t.Value);
            bag.Add(new CapturedMeasurement(instrument.Name, value, tagDict));
        });

        meterListener.Start();
        return (meterListener, bag);
    }

    // ─── Minimal projection helpers ───────────────────────────────────────────

    /// <summary>Projection that handles <see cref="SimpleOtelEvent"/> with a basic document update.</summary>
    private sealed class SimpleOtelProjection(IEnumerable<object> events, string? name = null)
        : InMemoryProjection<string, TestDocument<string>>
    {
        private readonly ImmutableList<EventWithPosition> _events =
            events.Select((e, i) => new EventWithPosition(e, i + 1)).ToImmutableList();

        public override string Name => name ?? nameof(SimpleOtelProjection);

        public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>>
            Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>> config)
        {
            return config
                .On<SimpleOtelEvent>().WithId(e => new SimpleIdContext<string>(e.DocId))
                .WhenAny(h => h.HandleWith((evnt, ctx, _, _) =>
                {
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    });
                    return Task.CompletedTask;
                }));
        }

        public override Task<IProjectionEventSource> GetSource()
        {
            var snapshot = _events;
            return Task.FromResult<IProjectionEventSource>(
                new SimpleProjectionEventSource((fromPosition, _) =>
                    Source.From(snapshot)
                        .Where(x => fromPosition == null || x.Position > fromPosition)));
        }
    }

    /// <summary>Projection whose handler always throws — used to drive <c>projection.events.failed</c>.</summary>
    private sealed class FailingHandlerOtelProjection(IEnumerable<object> events)
        : InMemoryProjection<string, TestDocument<string>>
    {
        private readonly ImmutableList<EventWithPosition> _events =
            events.Select((e, i) => new EventWithPosition(e, i + 1)).ToImmutableList();

        public override string Name => nameof(FailingHandlerOtelProjection);

        public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>>
            Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>> config)
        {
            return config
                .On<FailingHandlerOtelEvent>().WithId(e => new SimpleIdContext<string>(e.DocId))
                .WhenAny(h => h.HandleWith((_, _, _, _) =>
                    throw new InvalidOperationException("Deliberate handler failure for OTEL test")));
        }

        public override Task<IProjectionEventSource> GetSource()
        {
            var snapshot = _events;
            return Task.FromResult<IProjectionEventSource>(
                new SimpleProjectionEventSource((fromPosition, _) =>
                    Source.From(snapshot)
                        .Where(x => fromPosition == null || x.Position > fromPosition)));
        }
    }

    /// <summary>Projection that uses <c>WithData</c> with a successful <c>getData</c> call.</summary>
    private sealed class WithDataOtelProjection(IEnumerable<object> events)
        : InMemoryProjection<string, TestDocument<string>>
    {
        private readonly ImmutableList<EventWithPosition> _events =
            events.Select((e, i) => new EventWithPosition(e, i + 1)).ToImmutableList();

        public override string Name => nameof(WithDataOtelProjection);

        public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>>
            Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>> config)
        {
            return config
                .On<WithDataOtelEvent>()
                .WithData(e => Task.FromResult("fetched:" + e.DocId))
                .WithId((e, _) => new SimpleIdContext<string>(e.DocId))
                .WhenAny(h => h.HandleWith((evnt, ctx, data, _, _) =>
                {
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        doc.ReceivedData = doc.ReceivedData.Add(data);
                        return doc;
                    });
                    return Task.CompletedTask;
                }));
        }

        public override Task<IProjectionEventSource> GetSource()
        {
            var snapshot = _events;
            return Task.FromResult<IProjectionEventSource>(
                new SimpleProjectionEventSource((fromPosition, _) =>
                    Source.From(snapshot)
                        .Where(x => fromPosition == null || x.Position > fromPosition)));
        }
    }

    /// <summary>
    /// Projection whose <c>getData</c> always throws — used to drive
    /// <c>projection.withdata.fetch.failures</c> in the no-restart scenario.
    /// </summary>
    private sealed class FailingGetDataOtelProjection(IEnumerable<object> events)
        : InMemoryProjection<string, TestDocument<string>>
    {
        private readonly ImmutableList<EventWithPosition> _events =
            events.Select((e, i) => new EventWithPosition(e, i + 1)).ToImmutableList();

        public override string Name => nameof(FailingGetDataOtelProjection);

        public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>>
            Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>> config)
        {
            return config
                .On<WithDataOtelEvent>()
                .WithData(_ => Task.FromException<string>(
                    new InvalidOperationException("Deliberate getData failure for OTEL test")))
                .WithId((e, _) => new SimpleIdContext<string>(e.DocId))
                .WhenAny(h => h.HandleWith((_, _, _, _, _) => Task.CompletedTask));
        }

        public override Task<IProjectionEventSource> GetSource()
        {
            var snapshot = _events;
            return Task.FromResult<IProjectionEventSource>(
                new SimpleProjectionEventSource((fromPosition, _) =>
                    Source.From(snapshot)
                        .Where(x => fromPosition == null || x.Position > fromPosition)));
        }
    }

    /// <summary>
    /// Projection whose <c>getData</c> throws exactly once, then succeeds — used to verify
    /// the failure counter is incremented before a successful restart.
    /// </summary>
    private sealed class OnceFailingGetDataOtelProjection(IEnumerable<object> events)
        : InMemoryProjection<string, TestDocument<string>>
    {
        private readonly ImmutableList<EventWithPosition> _events =
            events.Select((e, i) => new EventWithPosition(e, i + 1)).ToImmutableList();

        private int _failuresRemaining = 1;

        // Short timeout so the Ask to the sequencer expires quickly after the actor
        // crash, allowing the coordinator's restart source to kick in fast enough for
        // the test to complete within its overall timeout.
        public override TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(1);

        public override string Name => nameof(OnceFailingGetDataOtelProjection);

        public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>>
            Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, TestDocument<string>>> config)
        {
            return config
                .On<WithDataOtelEvent>()
                .WithData(_ =>
                {
                    if (Interlocked.Decrement(ref _failuresRemaining) >= 0)
                        throw new InvalidOperationException("Once-failing getData for OTEL test");
                    return Task.FromResult("ok");
                })
                .WithId((e, _) => new SimpleIdContext<string>(e.DocId))
                .WhenAny(h => h.HandleWith((evnt, ctx, _, _, _) =>
                {
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    });
                    return Task.CompletedTask;
                }));
        }

        public override Task<IProjectionEventSource> GetSource()
        {
            var snapshot = _events;
            return Task.FromResult<IProjectionEventSource>(
                new SimpleProjectionEventSource((fromPosition, _) =>
                    Source.From(snapshot)
                        .Where(x => fromPosition == null || x.Position > fromPosition)));
        }
    }
}











