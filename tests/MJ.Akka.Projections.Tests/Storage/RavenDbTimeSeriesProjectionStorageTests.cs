using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Storage.RavenDb;
using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.Messages;
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
    private readonly Guid _eventId = Guid.NewGuid();

    protected override SetupRavenDbStorage GetStorage()
    {
        return new SetupRavenDbStorage(_documentStore, new BulkInsertOptions());
    }

    protected override StoreProjectionRequest CreateInsertRequest(string id)
    {
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
            new DocumentResults.DocumentCreated(id, new TestDocument
            {
                Id = id,
                HandledEvents = ImmutableList.Create(_eventId)
            }),
            new StoreTimeSeries(id, "test-series", ImmutableList.Create(
                new TimeSeriesRecord(_now, ImmutableList.Create(5d), "tag")))));
    }

    protected override StoreProjectionRequest CreateDeleteRequest(string id)
    {
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
            new DocumentResults.DocumentDeleted(id)));
    }
    
    protected override IProjection<string, RavenDbProjectionContext<TestDocument>, SetupRavenDbStorage> 
        CreateProjection()
    {
        return new TestProjection();
    }
    
    protected override async Task VerifyContext(RavenDbProjectionContext<TestDocument> loaded)
    {
        loaded.Document.Should().NotBeNull();
        loaded.Document!.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(_eventId));

        using var session = _documentStore.OpenAsyncSession();
        
        var document = await session.LoadAsync<TestDocument>(loaded.Id);

        document.Should().NotBeNull();
        document.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(_eventId));

        var timeSeriesValues = await session
            .TimeSeriesFor(loaded.Id, "test-series")
            .GetAsync();

        timeSeriesValues.Length.Should().Be(1);

        timeSeriesValues[0].Value.Should().Be(5d);
        timeSeriesValues[0].Tag.Should().Be("tag");
    }
    
    [PublicAPI]
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