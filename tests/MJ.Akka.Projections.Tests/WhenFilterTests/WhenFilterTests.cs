using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using AutoFixture;
using Shouldly;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;

namespace MJ.Akka.Projections.Tests.WhenFilterTests;

// Shared event / document types — public so they can be used as type args in public test classes.
public record TestEvent(string Id, string Tag);

public class Doc
{
    public List<string> Tags { get; } = [];
}

/// <summary>
/// Tests for the When() filter step between On() and HandleWith().
/// </summary>
public class WhenFilterTests
{
    private static readonly Fixture Fixture = new();

    // ---------------------------------------------------------------------------
    // 1. When() event filter — handler only runs when filter passes
    // ---------------------------------------------------------------------------

    public class WhenEventFilterPassesHandlerRuns(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();
        private readonly string _matchingTag = "run";

        [Fact]
        public void Then_only_matching_events_are_handled() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_id, _matchingTag);

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_id, _matchingTag),   // passes filter → handler runs
            new TestEvent(_id, "skip"),          // fails filter → handler skipped
        ];

        protected override async Task Then()
        {
            var ctx = await LoadContext(_id);
            ctx.Document!.Tags.ShouldHaveSingleItem().ShouldBe(_matchingTag);
        }

        private class Proj(string id, string matchingTag)
            : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(WhenEventFilterPassesHandlerRuns);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == id ? new SimpleIdContext<string>(e.Id) : null)
                    .When(f => f.WithEventFilter(e => e.Tag == matchingTag), h => h.HandleWith((evnt, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc =>
                        {
                            doc ??= new Doc();
                            doc.Tags.Add(evnt.Tag);
                            return doc;
                        });
                        return Task.CompletedTask;
                    }));

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 2. When() document filter — handler only runs when context condition holds
    // ---------------------------------------------------------------------------

    public class WhenDocumentFilterFailsHandlerIsSkipped(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _existingId = Fixture.Create<string>();
        private readonly string _newId = Fixture.Create<string>();

        [Fact]
        public void Then_conditional_handler_only_runs_when_document_condition_holds() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_existingId, _newId);

        // Seed one document so it already exists; the other starts empty.
        protected override IImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> Given()
        {
            var existingDoc = new Doc();
            existingDoc.Tags.Add("pre-existing");
            var ctx = new InMemoryProjectionContext<string, Doc>(
                new SimpleIdContext<string>(_existingId), existingDoc);
            return ImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>.Empty
                .Add(new SimpleIdContext<string>(_existingId), ctx);
        }

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_existingId, "e1"),   // doc exists      → filter passes → conditional handler runs
            new TestEvent(_newId,      "e2"),   // doc is new/null → filter fails  → conditional handler skipped
        ];

        protected override async Task Then()
        {
            var existingCtx = await LoadContext(_existingId);
            // existing doc: unconditional runs + conditional runs
            existingCtx.Document!.Tags.ShouldContain("unconditional");
            existingCtx.Document!.Tags.ShouldContain("conditional");

            var newCtx = await LoadContext(_newId);
            // new doc: unconditional runs, conditional skipped (doc didn't exist at load time)
            newCtx.Document!.Tags.ShouldContain("unconditional");
            newCtx.Document!.Tags.ShouldNotContain("conditional");
        }

        private class Proj(string existingId, string newId)
            : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(WhenDocumentFilterFailsHandlerIsSkipped);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == existingId || e.Id == newId ? new SimpleIdContext<string>(e.Id) : null)
                    .HandleWith((_, ctx, _, _) =>   // always runs
                    {
                        ctx.ModifyDocument(doc =>
                        {
                            doc ??= new Doc();
                            doc.Tags.Add("unconditional");
                            return doc;
                        });
                        return Task.CompletedTask;
                    })
                    .When(f => f.WithDocumentFilter(doc => doc.Exists()), h => h.HandleWith((_, ctx, _, _) =>   // only runs when document already existed at load time
                    {
                        ctx.ModifyDocument(doc =>
                        {
                            doc!.Tags.Add("conditional");
                            return doc;
                        });
                        return Task.CompletedTask;
                    }));

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 3. Two When().HandleWith() pairs each carry their own independent filter
    // ---------------------------------------------------------------------------

    public class TwoWhenHandleWithPairsHaveIndependentFilters(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();

        [Fact]
        public void Then_each_handleWith_uses_its_own_filter() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_id);

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_id, "alpha"),   // passes first filter only  → handler A fires
            new TestEvent(_id, "beta"),    // passes second filter only  → handler B fires
            new TestEvent(_id, "other"),   // passes neither filter      → neither fires
        ];

        protected override async Task Then()
        {
            var ctx = await LoadContext(_id);
            ctx.Document!.Tags.ShouldBe(new[] { "A", "B" });
        }

        private class Proj(string id)
            : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(TwoWhenHandleWithPairsHaveIndependentFilters);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == id ? new SimpleIdContext<string>(e.Id) : null)
                    .When(f => f.WithEventFilter(e => e.Tag == "alpha"), h => h.HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("A"); return doc; });
                        return Task.CompletedTask;
                    }))
                    .When(f => f.WithEventFilter(e => e.Tag == "beta"), h => h.HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("B"); return doc; });
                        return Task.CompletedTask;
                    }));

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 4. When() filter does not bleed into a subsequent HandleWith() without When()
    // ---------------------------------------------------------------------------

    public class WhenFilterDoesNotBleedIntoNextHandleWith(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();

        [Fact]
        public void Then_filter_only_applies_to_its_own_handleWith() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_id);

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_id, "skip"),   // fails When() filter → filtered handler skipped, unconditional runs
            new TestEvent(_id, "run"),    // passes When() filter → both handlers run
        ];

        protected override async Task Then()
        {
            var ctx = await LoadContext(_id);
            // "skip": filtered skipped, unconditional runs → ["unconditional"]
            // "run":  filtered runs, unconditional runs   → ["unconditional","filtered","unconditional"]
            ctx.Document!.Tags.ShouldBe(new[] { "unconditional", "filtered", "unconditional" });
        }

        private class Proj(string id)
            : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(WhenFilterDoesNotBleedIntoNextHandleWith);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == id ? new SimpleIdContext<string>(e.Id) : null)
                    .When(f => f.WithEventFilter(e => e.Tag == "run"), h => h.HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("filtered"); return doc; });
                        return Task.CompletedTask;
                    }))
                    .HandleWith((_, ctx, _, _) =>   // no When() → always runs
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("unconditional"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }
}







