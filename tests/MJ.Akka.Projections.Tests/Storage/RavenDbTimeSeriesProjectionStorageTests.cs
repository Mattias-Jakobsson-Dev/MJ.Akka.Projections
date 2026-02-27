using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Storage.RavenDb;
using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbTimeSeriesProjectionStorageTests(RavenDbFixture fixture) 
    : ProjectionStorageTests<
        SimpleIdContext<string>, 
        RavenDbProjectionContext<RavenDbTimeSeriesProjectionStorageTests.TestDocument, SimpleIdContext<string>>, 
        SetupRavenDbStorage>, IClassFixture<RavenDbFixture>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();
    private readonly DateTime _now = DateTime.Now;
    private readonly Guid _eventId = Guid.NewGuid();

    protected override SetupRavenDbStorage GetStorage()
    {
        return new SetupRavenDbStorage(_documentStore, new BulkInsertOptions());
    }

    protected override RavenDbProjectionContext<TestDocument, SimpleIdContext<string>> CreateInsertRequest(
        SimpleIdContext<string> id)
    {
        var context = new RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>(
            id,
            new TestDocument
            {
                Id = id,
                HandledEvents = ImmutableList.Create(_eventId)
            },
            ImmutableDictionary<string, object>.Empty);
        
        context.AddTimeSeries(new TimeSeriesInput(
            "test-series",
            _now,
            5,
            "tag"));

        return context;
    }

    protected override RavenDbProjectionContext<TestDocument, SimpleIdContext<string>> CreateDeleteRequest(
        SimpleIdContext<string> id)
    {
        return new RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>(
            id,
            null,
            ImmutableDictionary<string, object>.Empty);
    }

    protected override IProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>, SetupRavenDbStorage> 
        CreateProjection()
    {
        return new TestProjection();
    }
    
    protected override async Task VerifyContext(RavenDbProjectionContext<TestDocument, SimpleIdContext<string>> loaded)
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
    
    private class TestProjection : RavenDbProjection<TestDocument, SimpleIdContext<string>>
    {
        public override ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>> Configure(
            ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<TestDocument, SimpleIdContext<string>>> config)
        {
            return config;
        }

        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(ImmutableList<EventWithPosition>.Empty);
        }
    }
}