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
/// A composite id context with two fields — used to verify that complex id contexts
/// are preserved intact through all handler stages.
/// </summary>
public record CompositeIdContext(string TenantId, string EntityId) : IProjectionIdContext
{
    public bool Equals(IProjectionIdContext? other) =>
        other is CompositeIdContext c && c.TenantId == TenantId && c.EntityId == EntityId;

    public string GetStringRepresentation() => $"{TenantId}:{EntityId}";
}

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
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _existingId = Fixture.Create<string>();
        private readonly string _newId = Fixture.Create<string>();

        [Fact]
        public void Then_when_exists_handler_fires_only_for_pre_existing_document() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_existingId, _newId);

        protected override IImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>> Given()
        {
            var existing = new Doc();
            existing.Tags.Add("seed");
            var ctx = new InMemoryProjectionContext<SimpleIdContext<string>, Doc>(
                new SimpleIdContext<string>(_existingId), existing);
            return ImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>>.Empty
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

        private class Proj(string existingId, string newId) : InMemoryProjection<SimpleIdContext<string>, Doc>
        {
            public override string Name => nameof(WhenExistsHandlerOnlyRunsForExistingDocument);

            public override ISetupProjectionHandlers<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>> config) =>
                config
                    .On<TestEvent>(e => e.Id == existingId || e.Id == newId ? new SimpleIdContext<string>(e.Id) : null)
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    })
                    .WhenExists()
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc!.Tags.Add("exists-only"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 2. WhenNotExists — handler only runs when document does NOT yet exist
    // ---------------------------------------------------------------------------

    public class WhenNotExistsHandlerOnlyRunsForNewDocument(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _existingId = Fixture.Create<string>();
        private readonly string _newId = Fixture.Create<string>();

        [Fact]
        public void Then_when_not_exists_handler_fires_only_for_new_document() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_existingId, _newId);

        protected override IImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>> Given()
        {
            var existing = new Doc();
            existing.Tags.Add("seed");
            var ctx = new InMemoryProjectionContext<SimpleIdContext<string>, Doc>(
                new SimpleIdContext<string>(_existingId), existing);
            return ImmutableDictionary<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>>.Empty
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

        private class Proj(string existingId, string newId) : InMemoryProjection<SimpleIdContext<string>, Doc>
        {
            public override string Name => nameof(WhenNotExistsHandlerOnlyRunsForNewDocument);

            public override ISetupProjectionHandlers<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>> config) =>
                config
                    .On<TestEvent>(e => e.Id == existingId || e.Id == newId ? new SimpleIdContext<string>(e.Id) : null)
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    })
                    .WhenNotExists()
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("new-only"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 3. Multiple chained HandleWith() calls execute in order for every event
    // ---------------------------------------------------------------------------

    public class MultipleChainedHandlersExecuteInOrder(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();

        [Fact]
        public void Then_all_chained_handlers_run_in_declaration_order() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>
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

        private class Proj(string id) : InMemoryProjection<SimpleIdContext<string>, Doc>
        {
            public override string Name => nameof(MultipleChainedHandlersExecuteInOrder);

            public override ISetupProjectionHandlers<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>> config) =>
                config
                    .On<TestEvent>(e => e.Id == id ? new SimpleIdContext<string>(e.Id) : null)
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
        : AkkaProjectionsTestKit.ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _id = Fixture.Create<string>();

        [Fact]
        public void Then_only_matching_chained_handlers_fire() { }

        protected override IProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>, SetupInMemoryStorage>
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

        private class Proj(string id) : InMemoryProjection<SimpleIdContext<string>, Doc>
        {
            public override string Name => nameof(ChainedHandlersWithMixedFiltersRunSelectively);

            public override ISetupProjectionHandlers<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>>
                Configure(ISetupProjection<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, Doc>> config) =>
                config
                    .On<TestEvent>(e => e.Id == id ? new SimpleIdContext<string>(e.Id) : null)
                    .When(f => f.WithEventFilter(e => e.Tag == "alpha"))
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("A"); return doc; });
                        return Task.CompletedTask;
                    })
                    .When(f => f.WithEventFilter(e => e.Tag == "beta"))
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("B"); return doc; });
                        return Task.CompletedTask;
                    })
                    .HandleWith((_, ctx, _, _) =>   // no When() — always runs
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 5. Complex IProjectionIdContext — all fields flow intact to every handler
    // ---------------------------------------------------------------------------

    public class ComplexIdContextFlowsThroughAllHandlers(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _tenantId = Fixture.Create<string>();
        private readonly string _entityId = Fixture.Create<string>();

        [Fact]
        public void Then_composite_id_context_fields_are_visible_in_every_handler() { }

        protected override IProjection<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_tenantId, _entityId);

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_entityId, _tenantId),
        ];

        protected override async Task Then()
        {
            var ctx = await LoadContext(new CompositeIdContext(_tenantId, _entityId));
            // All three handlers should have captured both parts of the composite id
            ctx.Document!.Tags.ShouldBe(new[]
            {
                $"h1:{_tenantId}:{_entityId}",
                $"h2:{_tenantId}:{_entityId}",
                $"h3:{_tenantId}:{_entityId}",
            });
        }

        private class Proj(string tenantId, string entityId)
            : InMemoryProjection<CompositeIdContext, Doc>
        {
            public override string Name => nameof(ComplexIdContextFlowsThroughAllHandlers);

            public override ISetupProjectionHandlers<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>>
                Configure(ISetupProjection<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>> config) =>
                config
                    .On<TestEvent>(e => e.Tag == tenantId && e.Id == entityId
                        ? new CompositeIdContext(e.Tag, e.Id)
                        : null)
                    // Handler 1 — unconditional; reads composite id via context
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc =>
                        {
                            doc ??= new Doc();
                            doc.Tags.Add($"h1:{ctx.Id.TenantId}:{ctx.Id.EntityId}");
                            return doc;
                        });
                        return Task.CompletedTask;
                    })
                    // Handler 2 — with a When() filter, still receives the same composite id
                    .When(f => f.WithEventFilter(_ => true))
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc =>
                        {
                            doc!.Tags.Add($"h2:{ctx.Id.TenantId}:{ctx.Id.EntityId}");
                            return doc;
                        });
                        return Task.CompletedTask;
                    })
                    // Handler 3 — chained after handler 2, no filter
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc =>
                        {
                            doc!.Tags.Add($"h3:{ctx.Id.TenantId}:{ctx.Id.EntityId}");
                            return doc;
                        });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }

    // ---------------------------------------------------------------------------
    // 6. Complex id context with WhenExists — context fields still accessible
    // ---------------------------------------------------------------------------

    public class ComplexIdContextWithWhenExistsFilterIsCorrect(NormalTestKitActorSystem _)
        : AkkaProjectionsTestKit.ProjectionTestKit<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>, SetupInMemoryStorage>,
            IClassFixture<NormalTestKitActorSystem>
    {
        private readonly string _tenantId = Fixture.Create<string>();
        private readonly string _existingEntityId = Fixture.Create<string>();
        private readonly string _newEntityId = Fixture.Create<string>();

        [Fact]
        public void Then_when_exists_with_complex_id_context_works_correctly() { }

        protected override IProjection<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>, SetupInMemoryStorage>
            GetProjectionToTest() => new Proj(_tenantId, _existingEntityId, _newEntityId);

        protected override IImmutableDictionary<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>> Given()
        {
            var existing = new Doc();
            existing.Tags.Add("seed");
            var key = new CompositeIdContext(_tenantId, _existingEntityId);
            var ctx = new InMemoryProjectionContext<CompositeIdContext, Doc>(key, existing);
            return ImmutableDictionary<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>>.Empty
                .Add(key, ctx);
        }

        protected override IEnumerable<object> When() =>
        [
            new TestEvent(_existingEntityId, _tenantId),
            new TestEvent(_newEntityId, _tenantId),
        ];

        protected override async Task Then()
        {
            var existingCtx = await LoadContext(new CompositeIdContext(_tenantId, _existingEntityId));
            existingCtx.Document!.Tags.ShouldContain("always");
            existingCtx.Document!.Tags.ShouldContain("exists-only");

            var newCtx = await LoadContext(new CompositeIdContext(_tenantId, _newEntityId));
            newCtx.Document!.Tags.ShouldContain("always");
            newCtx.Document!.Tags.ShouldNotContain("exists-only");
        }

        private class Proj(string tenantId, string existingEntityId, string newEntityId)
            : InMemoryProjection<CompositeIdContext, Doc>
        {
            public override string Name => nameof(ComplexIdContextWithWhenExistsFilterIsCorrect);

            public override ISetupProjectionHandlers<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>>
                Configure(ISetupProjection<CompositeIdContext, InMemoryProjectionContext<CompositeIdContext, Doc>> config) =>
                config
                    .On<TestEvent>(e => e.Tag == tenantId && (e.Id == existingEntityId || e.Id == newEntityId)
                        ? new CompositeIdContext(e.Tag, e.Id)
                        : null)
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc ??= new Doc(); doc.Tags.Add("always"); return doc; });
                        return Task.CompletedTask;
                    })
                    .WhenExists()
                    .HandleWith((_, ctx, _, _) =>
                    {
                        ctx.ModifyDocument(doc => { doc!.Tags.Add("exists-only"); return doc; });
                        return Task.CompletedTask;
                    });

            public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) =>
                Source.Empty<EventWithPosition>();
        }
    }
}

