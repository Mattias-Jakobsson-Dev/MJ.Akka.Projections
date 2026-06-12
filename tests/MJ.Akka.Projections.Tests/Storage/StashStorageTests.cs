using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;
using Shouldly;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public abstract class StashStorageTests
{
    private static ProjectionContextId NewId() =>
        new(Guid.NewGuid().ToString(), new SimpleIdContext<string>(Guid.NewGuid().ToString()));

    private static ImmutableList<EventWithPosition> Events(params int[] positions) =>
        positions
            .Select(p => new EventWithPosition(new TestEvent(p), p))
            .ToImmutableList();

    protected abstract IProjectionStashStorage GetStorage();
    protected abstract IProjectionStashStorage GetStorageWithTimeout(TimeSpan timeout);

    // ── Stash ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unstash_from_empty_stash_returns_empty()
    {
        var storage = GetStorage();
        var id = NewId();

        var (events, token) = await storage.Unstash(id);

        events.ShouldBeEmpty();
        token.Count.ShouldBe(0u);
    }

    [Fact]
    public async Task Stashed_events_are_returned_by_unstash()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3));

        var (events, _) = await storage.Unstash(id);

        events.Count.ShouldBe(3);
        events.Select(e => e.Position).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Stash_appends_to_existing_events()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2));
        await storage.Stash(id, Events(3, 4));

        var (events, _) = await storage.Unstash(id);

        events.Count.ShouldBe(4);
        events.Select(e => e.Position).ShouldBe([1, 2, 3, 4]);
    }

    // ── Unstash count limiting ───────────────────────────────────────────────

    [Fact]
    public async Task Unstash_respects_numberOfEvents_limit()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3, 4, 5));

        var (events, token) = await storage.Unstash(id, numberOfEvents: 3);

        events.Count.ShouldBe(3);
        token.Count.ShouldBe(3u);
        events.Select(e => e.Position).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Unstash_with_limit_larger_than_available_returns_all()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2));

        var (events, token) = await storage.Unstash(id, numberOfEvents: 10);

        events.Count.ShouldBe(2);
        token.Count.ShouldBe(2u);
    }

    // ── In-process / peek semantics ──────────────────────────────────────────

    [Fact]
    public async Task Events_not_removed_before_ack()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3));
        var (_, token) = await storage.Unstash(id);

        // Without ack there should still be events – but they are in-process
        // so a second unstash before ack returns nothing
        var (secondPeek, _) = await storage.Unstash(id);
        secondPeek.ShouldBeEmpty();

        // After ack the events are gone
        await storage.AckUnstash(token);

        var (afterAck, _) = await storage.Unstash(id);
        afterAck.ShouldBeEmpty();
    }

    [Fact]
    public async Task AckUnstash_removes_exactly_the_in_process_events()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3, 4, 5));

        var (_, token) = await storage.Unstash(id, numberOfEvents: 2);
        await storage.AckUnstash(token);

        var (remaining, _) = await storage.Unstash(id);
        remaining.Count.ShouldBe(3);
        remaining.Select(e => e.Position).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public async Task NackUnstash_makes_events_available_again()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3));
        var (_, token) = await storage.Unstash(id);

        await storage.NackUnstash(token);

        var (events, _) = await storage.Unstash(id);
        events.Count.ShouldBe(3);
        events.Select(e => e.Position).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task AckUnstash_with_zero_token_is_noop()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1));
        var emptyToken = new StashToken(id, 0);

        await storage.AckUnstash(emptyToken); // should not throw or remove anything

        var (events, _) = await storage.Unstash(id);
        events.Count.ShouldBe(1);
    }

    [Fact]
    public async Task NackUnstash_with_zero_token_is_noop()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1));
        var emptyToken = new StashToken(id, 0);

        await storage.NackUnstash(emptyToken);

        var (events, _) = await storage.Unstash(id);
        events.Count.ShouldBe(1);
    }

    // ── Isolation between ids ────────────────────────────────────────────────

    [Fact]
    public async Task Stash_and_unstash_are_isolated_per_id()
    {
        var storage = GetStorage();
        var id1 = NewId();
        var id2 = NewId();

        await storage.Stash(id1, Events(1, 2));
        await storage.Stash(id2, Events(10, 20));

        var (events1, token1) = await storage.Unstash(id1);
        var (events2, _) = await storage.Unstash(id2);

        events1.Select(e => e.Position).ShouldBe([1, 2]);
        events2.Select(e => e.Position).ShouldBe([10, 20]);

        await storage.AckUnstash(token1);

        var (afterAck1, _) = await storage.Unstash(id1);
        var (afterAck2, _) = await storage.Unstash(id2);

        afterAck1.ShouldBeEmpty();
        afterAck2.ShouldBeEmpty(); // still in-process for id2
    }

    // ── Document / record cleanup after full ack ────────────────────────────

    [Fact]
    public async Task AckUnstash_all_events_leaves_stash_permanently_empty()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3));
        var (_, token) = await storage.Unstash(id);
        await storage.AckUnstash(token);

        // Stash is gone; a subsequent unstash must return nothing
        var (afterAck, _) = await storage.Unstash(id);
        afterAck.ShouldBeEmpty();
    }

    [Fact]
    public async Task After_full_ack_new_events_can_be_stashed_again()
    {
        var storage = GetStorage();
        var id = NewId();

        await storage.Stash(id, Events(1, 2));
        var (_, token) = await storage.Unstash(id);
        await storage.AckUnstash(token);

        // Re-stash new events and verify they are returned correctly
        await storage.Stash(id, Events(10, 20));
        var (events, _) = await storage.Unstash(id);
        events.Count.ShouldBe(2);
        events.Select(e => e.Position).ShouldBe([10, 20]);
    }

    // ── In-process timeout / expiry ──────────────────────────────────────────

    [Fact]
    public async Task Expired_in_process_claim_is_automatically_released_on_next_unstash()
    {
        var storage = GetStorageWithTimeout(TimeSpan.FromMilliseconds(50));
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3));
        var (_, _) = await storage.Unstash(id); // claim events

        // Wait for the timeout to elapse
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Next unstash should auto-expire the stale claim and return events
        var (events, token) = await storage.Unstash(id);
        events.Count.ShouldBe(3);
        token.Count.ShouldBe(3u);
    }

    [Fact]
    public async Task Non_expired_in_process_claim_blocks_second_unstash()
    {
        var storage = GetStorageWithTimeout(TimeSpan.FromSeconds(60));
        var id = NewId();

        await storage.Stash(id, Events(1, 2, 3));
        var (_, _) = await storage.Unstash(id); // claim events

        var (events, token) = await storage.Unstash(id);
        events.ShouldBeEmpty();
        token.Count.ShouldBe(0u);
    }

    private record TestEvent(int Value);
}

