using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Shouldly;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

/// <summary>
/// Verifies that backpressure propagates all the way back to the event source in the
/// coordinator pipeline.
///
/// Two complementary angles:
///  1. Direct Akka.Streams verification: Ask(parallelism=1) gates upstream demand.
///  2. Full coordinator pipeline: slow handlers prevent the source from being exhausted.
/// </summary>
public class When_source_is_backpressured_by_slow_processing
    : global::Akka.TestKit.Xunit2.TestKit
{
    // ── Test 1: Akka.Streams Ask backpressure ─────────────────────────────────

    /// <summary>
    /// With Ask(parallelism=1), the operator must not request the next element from
    /// upstream until the previous actor reply has been received.
    ///
    /// We record the wall-clock time at which each source element is pulled and then
    /// assert that consecutive pulls are separated by approximately the actor's
    /// response delay, proving that the source is gated rather than pre-fetched.
    /// </summary>
    [Fact]
    public async Task Ask_with_parallelism_1_only_requests_next_element_after_previous_response_arrives()
    {
        const int elementCount = 5;
        const int delayMs = 200;
        const int toleranceMs = 60;

        var emitTimes = new ConcurrentQueue<DateTime>();

        var source = Source
            .From(Enumerable.Range(1, elementCount))
            .Select(x =>
            {
                emitTimes.Enqueue(DateTime.UtcNow);
                return x;
            });

        var delayActor = Sys.ActorOf(Props.Create(() => new DelayEchoActor(TimeSpan.FromMilliseconds(delayMs))));

        // Run the full pipeline to completion.
        var results = await source
            .Ask<int>(delayActor, TimeSpan.FromSeconds(30), parallelism: 1)
            .RunWith(Sink.Seq<int>(), Sys.Materializer());

        // All elements arrive in order.
        results.ShouldBe(Enumerable.Range(1, elementCount).ToImmutableList());

        var times = emitTimes.ToList();
        times.Count.ShouldBe(elementCount);

        // With parallelism=1, the source must not emit element i+1 until element i's
        // reply arrives (~200 ms later). Each consecutive gap must be at least
        // (delayMs - tolerance) ms.
        for (var i = 1; i < times.Count; i++)
        {
            var gap = times[i] - times[i - 1];
            gap.ShouldBeGreaterThan(
                TimeSpan.FromMilliseconds(delayMs - toleranceMs),
                $"Gap between emission {i - 1} and {i} was {gap.TotalMilliseconds:F0} ms " +
                $"(expected ≥ {delayMs - toleranceMs} ms). " +
                "Ask(parallelism=1) should gate upstream demand.");
        }
    }

    // ── Test 2: Full coordinator pipeline backpressure ────────────────────────

    /// <summary>
    /// With 20 unique events, 500 ms per handler, and BatchEventBatchingStrategy(2):
    ///   • Only 2 document actors run at a time (global concurrency = parallelism = 2).
    ///   • The Batch operator pre-fetches at most 1 extra batch from the source while
    ///     the previous batch is being processed.
    ///   • SelectMany can buffer at most 1 batch's tasks while SelectAsync is saturated.
    ///
    /// Together this gives a theoretical upper bound of ≤ 3 batches × 2 events = 6
    /// events pulled from source while the first batch of slow handlers is running.
    /// We use 10 as a generous bound to accommodate scheduler jitter.
    ///
    /// Without backpressure the entire source of 20 events would be drained instantly.
    /// </summary>
    [Fact]
    public async Task Source_is_not_exhausted_while_handlers_are_slow()
    {
        const int totalEvents = 20;
        const int batchSize = 2;
        const int observationMs = 150; // well before 500 ms handlers finish
        const int maxExpectedAtObservation = 10; // generous upper bound (theory ≤ 6)

        var sourceEmitCount = 0;

        // 20 unique document IDs – every handler always delays (WhenAny path).
        var events = Enumerable.Range(1, totalEvents)
            .Select(i => new EventWithPosition(
                new SlowHandlerEvent($"doc-{i}", $"evt-{i}"),
                i))
            .ToList();

        var projection = new SlowHandlerProjection(
            events,
            delay: TimeSpan.FromMilliseconds(500),
            onEmit: () => Interlocked.Increment(ref sourceEmitCount));

        using var system = ActorSystem.Create(Sys.Name, DefaultConfig);

        var coordinator = await system
            .Projections(
                config => config
                    .WithProjection(projection)
                    .WithEventBatchingStrategy(new BatchEventBatchingStrategy(batchSize))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                new SetupInMemoryStorage())
            .Start();

        // Observe the source emit count mid-flight (handlers still running).
        await Task.Delay(observationMs);
        var emitCountAtObservation = Volatile.Read(ref sourceEmitCount);

        // Wait for the projection to finish, then check the final count.
        await coordinator.Get(projection.Name)!.WaitForCompletion(TimeSpan.FromSeconds(30));
        var finalEmitCount = Volatile.Read(ref sourceEmitCount);

        // Sanity: every event was eventually emitted.
        finalEmitCount.ShouldBe(totalEvents);

        // Core assertion: the source was NOT pre-drained while handlers were busy.
        // If backpressure were absent all 20 would be emitted at t ≈ 0.
        emitCountAtObservation.ShouldBeLessThan(totalEvents,
            $"All {totalEvents} events were pulled from the source by t={observationMs} ms, " +
            "indicating backpressure is not propagating to the source.");

        emitCountAtObservation.ShouldBeLessThanOrEqualTo(maxExpectedAtObservation,
            $"Source emitted {emitCountAtObservation} events at t={observationMs} ms " +
            $"(upper bound: {maxExpectedAtObservation}). Backpressure may not be working.");
    }

    // ── Supporting types ──────────────────────────────────────────────────────

    /// <summary>
    /// Actor that waits <paramref name="delay"/> before echoing back the received int.
    /// Used to simulate a slow sequencer in Test 1.
    /// </summary>
    private sealed class DelayEchoActor : ReceiveActor
    {
        public DelayEchoActor(TimeSpan delay)
        {
            ReceiveAsync<int>(async msg =>
            {
                await Task.Delay(delay);
                Sender.Tell(msg);
            });
        }
    }

    private record SlowHandlerEvent(string DocId, string EventId);

    /// <summary>
    /// Projection where every event handler always delays by <paramref name="delay"/>,
    /// regardless of whether the document already exists.  A callback
    /// <paramref name="onEmit"/> is invoked each time the source emits an event.
    /// </summary>
    private sealed class SlowHandlerProjection(
        IReadOnlyList<EventWithPosition> events,
        TimeSpan delay,
        Action onEmit)
        : InMemoryProjection<string, TestDocument<string>>
    {
        public override string Name => "SlowHandlerProjection";

        public override ISetupProjection<
            SimpleIdContext<string>,
            InMemoryProjectionContext<string, TestDocument<string>>>
            Configure(ISetupProjection<
                SimpleIdContext<string>,
                InMemoryProjectionContext<string, TestDocument<string>>> config)
        {
            return config
                .On<SlowHandlerEvent>()
                .WithId(e => new SimpleIdContext<string>(e.DocId))
                .WhenAny(h => h.HandleWith(async (evnt, ctx, _, ct) =>
                {
                    await Task.Delay(delay, ct);

                    ctx.ModifyDocument(doc =>
                    {
                        var d = doc ?? new TestDocument<string> { Id = evnt.DocId };
                        d.AddHandledEvent(evnt.EventId);
                        return d;
                    });
                }));
        }

        public override Task<IProjectionEventSource> GetSource() =>
            Task.FromResult<IProjectionEventSource>(
                new SimpleProjectionEventSource((fromPos, _) =>
                    Source
                        .From(events)
                        .Select(e =>
                        {
                            onEmit();
                            return e;
                        })
                        .Where(e => fromPos == null || e.Position > fromPos)));
    }
}



