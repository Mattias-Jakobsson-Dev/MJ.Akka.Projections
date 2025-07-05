using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Storage.RavenDb;
using FluentAssertions;
using JetBrains.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbTimeSeriesProjectionStorageTests(RavenDbFixture fixture) 
    : ProjectionStorageTests<
        string, 
        RavenDbProjectionContext<RavenDbTimeSeriesProjectionStorageTests.TestDocument>, 
        SetupRavenDbStorage>, IClassFixture<RavenDbFixture>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();
    private readonly DateTime _now = DateTime.Now;

    protected override SetupRavenDbStorage GetStorage()
    {
        return new SetupRavenDbStorage(fixture.OpenDocumentStore(), new BulkInsertOptions());
    }

    protected override RavenDbProjectionContext<TestDocument> CreateTestContext(string id)
    {
        var context = new RavenDbProjectionContext<TestDocument>(id, new TestDocument
        {
            Id = id,
            HandledEvents = ImmutableList.Create(Guid.NewGuid())
        });

        context.WithTimeSeriesValues(
            "test-series",
            _now,
            ImmutableList.Create(5d),
            "tag");

        return context;
    }

    protected override RavenDbProjectionContext<TestDocument> Delete(
        RavenDbProjectionContext<TestDocument> context)
    {
        context.ModifyDocument(_ => null);

        return context;
    }

    protected override IProjection<string, RavenDbProjectionContext<TestDocument>, SetupRavenDbStorage> 
        CreateProjection()
    {
        return new TestProjection();
    }

    protected override async Task VerifyContext(
        RavenDbProjectionContext<TestDocument> original, 
        RavenDbProjectionContext<TestDocument> loaded)
    {
        loaded.Document.Should().NotBeNull();
        loaded.Document!.Id.Should().Be(original.Document!.Id);
        loaded.Document.HandledEvents.Should().BeEquivalentTo(original.Document.HandledEvents);

        using var session = _documentStore.OpenAsyncSession();

        var timeSeriesValues = await session
            .TimeSeriesFor(loaded.Id, "test-series")
            .GetAsync();

        timeSeriesValues.Length.Should().Be(1);

        timeSeriesValues[0].Value.Should().Be(5d);
        timeSeriesValues[0].Tag.Should().Be("tag");
    }
    
    public class TestDocument
    {
        public required string Id { get; set; }
        public required IImmutableList<Guid> HandledEvents { get; set; }
    }
    
    private class TestProjection : RavenDbProjection<TestDocument>
    {
        public override ISetupProjection<string, RavenDbProjectionContext<TestDocument>> Configure(
            ISetupProjection<string, RavenDbProjectionContext<TestDocument>> config)
        {
            return config;
        }

        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(ImmutableList<EventWithPosition>.Empty);
        }
    }
}