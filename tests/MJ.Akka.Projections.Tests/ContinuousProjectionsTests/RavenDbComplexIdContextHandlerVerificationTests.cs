using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.RavenDb;
using MJ.Akka.Projections.Tests.Storage;
using MJ.Akka.Projections.Tests.TestData;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Shouldly;
using Xunit;
using AutoFixture;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class RavenDbComplexIdContextHandlerVerificationTests(
    RavenDbFixture ravenDbFixture,
    NormalTestKitActorSystem actorSystemSetup)
    : IClassFixture<RavenDbFixture>, IClassFixture<NormalTestKitActorSystem>
{
    private readonly Fixture _fixture = new();
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
    private readonly IDocumentStore _documentStore = ravenDbFixture.OpenDocumentStore();

    private SetupRavenDbStorage CreateStorageSetup() =>
        new(_documentStore, new BulkInsertOptions());

    [Fact]
    public async Task RavenDb_complex_id_context_fields_are_passed_to_HandleWith_handler()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var events = ImmutableList.Create<object>(
            new Events<string>.FirstEvent(docId, _fixture.Create<string>()));

        var projection = new RavenDbComplexIdContextVerifyingProjection(events, idContext);
        var storageSetup = CreateStorageSetup();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        projection.ReceivedContexts.ShouldNotBeEmpty();
        foreach (var receivedContext in projection.ReceivedContexts)
        {
            receivedContext.DocumentId.ShouldBe(docId);
            receivedContext.Child.ShouldNotBeNull();
            receivedContext.Child.Data.ShouldBe(childData);
        }
    }

    [Fact]
    public async Task RavenDb_complex_id_context_fields_are_passed_to_second_chained_HandleWith_handler()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var events = ImmutableList.Create<object>(
            new Events<string>.FirstEvent(docId, _fixture.Create<string>()),
            new Events<string>.FirstEvent(docId, _fixture.Create<string>()));

        var projection = new RavenDbComplexIdContextVerifyingProjection(events, idContext);
        var storageSetup = CreateStorageSetup();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        projection.ReceivedContextsViaSecondHandler.ShouldNotBeEmpty();
        foreach (var receivedContext in projection.ReceivedContextsViaSecondHandler)
        {
            receivedContext.DocumentId.ShouldBe(docId);
            receivedContext.Child.ShouldNotBeNull();
            receivedContext.Child.Data.ShouldBe(childData);
        }
    }

    [Fact]
    public async Task RavenDb_complex_id_context_fields_are_passed_to_all_chained_handlers()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var events = Enumerable.Range(0, 5)
            .Select(_ => (object)new Events<string>.FirstEvent(docId, _fixture.Create<string>()))
            .ToImmutableList();

        var projection = new RavenDbComplexIdContextVerifyingProjection(events, idContext);
        var storageSetup = CreateStorageSetup();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        // Both chained HandleWith calls should each fire once per event
        projection.ReceivedContexts.Count.ShouldBe(events.Count);
        projection.ReceivedContextsViaSecondHandler.Count.ShouldBe(events.Count);

        foreach (var ctx in projection.ReceivedContexts.Concat(projection.ReceivedContextsViaSecondHandler))
        {
            ctx.DocumentId.ShouldBe(docId);
            ctx.Child.Data.ShouldBe(childData);
        }
    }

    [Fact]
    public async Task RavenDb_complex_id_context_fields_are_passed_to_ModifyDocument_with_metadata()
    {
        using var system = actorSystemSetup.StartNewActorSystem();

        var childData = _fixture.Create<string>();
        var docId = _fixture.Create<string>();
        var idContext = new ProjectionWithComplexIdContextTests.ComplexIdContext(
            docId,
            new ProjectionWithComplexIdContextTests.ComplexChildObject(childData));

        var events = Enumerable.Range(0, 3)
            .Select(_ => (object)new Events<string>.FirstEvent(docId, _fixture.Create<string>()))
            .ToImmutableList();

        var projection = new RavenDbComplexIdContextVerifyingProjection(events, idContext);
        var storageSetup = CreateStorageSetup();

        var coordinator = await system
            .Projections(config => config.WithProjection(projection), storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(_timeout);

        projection.ReceivedContextsViaModifyDocument.Count.ShouldBe(events.Count);
        foreach (var ctx in projection.ReceivedContextsViaModifyDocument)
        {
            ctx.DocumentId.ShouldBe(docId);
            ctx.Child.Data.ShouldBe(childData);
        }
    }

    /// <summary>
    /// A RavenDB projection with three chained handlers for the same event type:
    /// 1. HandleWith — captures ctx.Id into ReceivedContexts and creates/updates the document.
    /// 2. HandleWith — captures ctx.Id into ReceivedContextsViaSecondHandler.
    /// 3. ModifyDocument with DocumentHandlingMetaData — captures metadata.Id into ReceivedContextsViaModifyDocument.
    ///
    /// All three are registered under a single On&lt;&gt; chain so none replaces another.
    /// </summary>
    private class RavenDbComplexIdContextVerifyingProjection(
        IEnumerable<object> events,
        ProjectionWithComplexIdContextTests.ComplexIdContext idContext)
        : RavenDbProjection<TestDocument<string>, ProjectionWithComplexIdContextTests.ComplexIdContext>
    {
        public override string Name => "RavenDbComplexIdContextVerifyingProjection";
        public override TimeSpan ProjectionTimeout => TimeSpan.FromSeconds(10);

        public ConcurrentBag<ProjectionWithComplexIdContextTests.ComplexIdContext> ReceivedContexts { get; } = new();
        public ConcurrentBag<ProjectionWithComplexIdContextTests.ComplexIdContext> ReceivedContextsViaSecondHandler { get; } = new();
        public ConcurrentBag<ProjectionWithComplexIdContextTests.ComplexIdContext> ReceivedContextsViaModifyDocument { get; } = new();

        private readonly ImmutableList<EventWithPosition> _events = events
            .Select((x, i) => new EventWithPosition(x, i + 1))
            .ToImmutableList();

        public override ISetupProjection<
                ProjectionWithComplexIdContextTests.ComplexIdContext,
                RavenDbProjectionContext<TestDocument<string>, ProjectionWithComplexIdContextTests.ComplexIdContext>>
            Configure(ISetupProjection<
                ProjectionWithComplexIdContextTests.ComplexIdContext,
                RavenDbProjectionContext<TestDocument<string>, ProjectionWithComplexIdContextTests.ComplexIdContext>> config)
        {
            return config
                .On<Events<string>.FirstEvent>().WithId(_ => idContext)
                // First handler: captures the full id context, creates/updates the document
                .HandleWith((evnt, ctx, _, _) =>
                {
                    ReceivedContexts.Add(ctx.Id);
                    ctx.ModifyDocument(doc =>
                    {
                        doc ??= new TestDocument<string> { Id = evnt.DocId };
                        doc.AddHandledEvent(evnt.EventId);
                        return doc;
                    });
                    return Task.CompletedTask;
                })
                // Second handler: captures ctx.Id directly
                .HandleWith((_, ctx, _, _) =>
                {
                    ReceivedContextsViaSecondHandler.Add(ctx.Id);
                    return Task.CompletedTask;
                })
                // Third handler: captures via DocumentHandlingMetaData (the RavenDb ModifyDocument overload)
                .ModifyDocument((_, doc, metadata) =>
                {
                    ReceivedContextsViaModifyDocument.Add(metadata.Id);
                    return doc!;
                });
        }

        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(_events
                .Where(x => fromPosition == null || x.Position > fromPosition)
                .ToImmutableList());
        }
    }
}


