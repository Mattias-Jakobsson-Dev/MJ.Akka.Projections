using System.Collections.Immutable;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Tests.TestData;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbTimeSeriesProjectionStorageTests(RavenDbFixture fixture) 
    : ProjectionStorageTests<string, DocumentWithTimeSeries<TestDocument<string>>>, IClassFixture<RavenDbFixture>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();
    private readonly DateTime _now = DateTime.Now;
    
    protected override IProjectionStorage GetStorage()
    {
        return new RavenDbProjectionStorage(_documentStore, new BulkInsertOptions());
    }

    protected override DocumentWithTimeSeries<TestDocument<string>> CreateTestDocument(string id)
    {
        return new DocumentWithTimeSeries<TestDocument<string>>(
            new TestDocument<string>
            {
                Id = id,
                HandledEvents = ImmutableList.Create(Guid.NewGuid().ToString())
            },
            new Dictionary<string, IImmutableList<TimeSeriesRecord>>
            {
                ["test-series"] = ImmutableList.Create(
                    new TimeSeriesRecord(_now, ImmutableList.Create(5d), "tag"))
            }.ToImmutableDictionary());
    }

    protected override async Task VerifyDocument(
        DocumentWithTimeSeries<TestDocument<string>> original,
        DocumentWithTimeSeries<TestDocument<string>> loaded)
    {
        loaded.Document.Should().NotBeNull();
        loaded.Document.Id.Should().Be(original.Document.Id);
        loaded.Document.HandledEvents.Should().BeEquivalentTo(original.Document.HandledEvents);
        loaded.TimeSeries.Count.Should().Be(0);
        
        using var session = _documentStore.OpenAsyncSession();

        var timeSeriesValues = await session
            .TimeSeriesFor(loaded.Document.Id, "test-series")
            .GetAsync();

        timeSeriesValues.Length.Should().Be(1);

        timeSeriesValues[0].Value.Should().Be(5d);
        timeSeriesValues[0].Tag.Should().Be("tag");
    }
}