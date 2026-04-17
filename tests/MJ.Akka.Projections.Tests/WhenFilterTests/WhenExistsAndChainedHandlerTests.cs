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


/// <summary>
/// Tests for WhenExists / WhenNotExists extensions and chained handlers.
/// </summary>
public class WhenExistsAndChainedHandlerTests
{
    private static readonly Fixture Fixture = new();

    // ---------------------------------------------------------------------------
    // 1. WhenExists — handler only runs when document already exists
    // ---------------------------------------------------------------------------

    public class WhenExistsHandlerOnlyRunsForExistingDocument(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _existingId = Fixture.Create<string>();
        private readonly string _newId = Fixture.Create<string>();

        [Fact]
        public void Then_when_exists_handler_fires_only_for_pre_existing_document() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_existingId, _newId);

        protected override IImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> Given()
        {
            var existing = new Doc();
            existing.Tags.Add("seed");
            var ctx = new InMemoryProjectionContext<string, Doc>(
                new SimpleIdContext<string>(_existingId), existing);
            return ImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>.Empty
                .Add(new SimpleIdContext<string>(_existingId), ctx);
        }

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_existingId, "e"),
            new TestEvent(_newId, "e"),
        ];

        protected override async Task Then()
        {
            var existingCtx = await LoadContext(_existingId);
            existingCtx.Document!.Tags.ShouldContain("always");
            existingCtx.Document!.Tags.ShouldContain("exists-only");

            var newCtx = await LoadContext(_newId);
            newCtx.Document!.Tags.ShouldContain("always");
            newCtx.Document!.Tags.ShouldNotContain("exists-only");
        }

        private class Proj(string existingId, string newId) : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(WhenExistsHandlerOnlyRunsForExistingDocument);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == existingId || e.Id == newId ? new SimpleIdContext<string>(e.Id) : null)
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    })
                    .WhenExists(h => h.HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc!.Tags.Add("exists-only"); return doc; });
                        return Task.CompletedTask;
                    }));

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 2. WhenNotExists — handler only runs when document does NOT yet exist
    // ---------------------------------------------------------------------------

    public class WhenNotExistsHandlerOnlyRunsForNewDocument(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _existingId = Fixture.Create<string>();
        private readonly string _newId = Fixture.Create<string>();

        [Fact]
        public void Then_when_not_exists_handler_fires_only_for_new_document() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_existingId, _newId);

        protected override IImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> Given()
        {
            var existing = new Doc();
            existing.Tags.Add("seed");
            var ctx = new InMemoryProjectionContext<string, Doc>(
                new SimpleIdContext<string>(_existingId), existing);
            return ImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>.Empty
                .Add(new SimpleIdContext<string>(_existingId), ctx);
        }

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_existingId, "e"),
            new TestEvent(_newId, "e"),
        ];

        protected override async Task Then()
        {
            var existingCtx = await LoadContext(_existingId);
            existingCtx.Document!.Tags.ShouldContain("always");
            existingCtx.Document!.Tags.ShouldNotContain("new-only");

            var newCtx = await LoadContext(_newId);
            newCtx.Document!.Tags.ShouldContain("always");
            newCtx.Document!.Tags.ShouldContain("new-only");
        }

        private class Proj(string existingId, string newId) : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(WhenNotExistsHandlerOnlyRunsForNewDocument);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == existingId || e.Id == newId ? new SimpleIdContext<string>(e.Id) : null)
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    })
                    .WhenNotExists(h => h.HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("new-only"); return doc; });
                        return Task.CompletedTask;
                    }));

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 3. Multiple chained HandleWith() calls execute in order for every event
    // ---------------------------------------------------------------------------

    public class MultipleChainedHandlersExecuteInOrder(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();

        [Fact]
        public void Then_all_chained_handlers_run_in_declaration_order() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_id);

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_id, "x"),
            new TestEvent(_id, "x"),
        ];

        protected override async Task Then()
        {
            var ctx = await LoadContext(_id);
            // Each event appends A, B, C in order → two events → 6 entries
            ctx.Document!.Tags.ShouldBe(new[] { "A", "B", "C", "A", "B", "C" });
        }

        private class Proj(string id) : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(MultipleChainedHandlersExecuteInOrder);

            public override ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>> config) =>
                config
                    .On<TestEvent>().WithId(e => e.Id == id ? new SimpleIdContext<string>(e.Id) : null)
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("A"); return doc; });
                        return Task.CompletedTask;
                    })
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc!.Tags.Add("B"); return doc; });
                        return Task.CompletedTask;
                    })
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc!.Tags.Add("C"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 4. Chained handlers with mixed When() filters — each filter is independent
    //    and a handler without When() always runs
    // ---------------------------------------------------------------------------

    public class ChainedHandlersWithMixedFiltersRunSelectively(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();

        [Fact]
        public void Then_only_matching_chained_handlers_fire() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<string, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_id);

        // Event "alpha" → filtered-A fires, always fires; filtered-B skipped
        // Event "beta"  → filtered-A skipped, always fires; filtered-B fires
        // Event "other" → filtered-A skipped, always fires; filtered-B skipped
        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_id, "alpha"),
            new TestEvent(_id, "beta"),
            new TestEvent(_id, "other"),
        ];

        protected override async Task Then()
        {
            var ctx = await LoadContext(_id);
            ctx.Document!.Tags.ShouldBe(new[]
            {
                "A",       // alpha → filtered-A
                "always",  // alpha → always
                "B",       // beta  → filtered-B
                "always",  // beta  → always
                "always",  // other → always
            });
        }

        private class Proj(string id) : InMemoryProjection<string, Doc>
        {
            public override string Name => nameof(ChainedHandlersWithMixedFiltersRunSelectively);

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
                    }))
                    .HandleWith((_, ctx, _, _) =>   // no When() — always runs
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

}
