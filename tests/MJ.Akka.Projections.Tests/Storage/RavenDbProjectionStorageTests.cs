using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using FluentAssertions;
using MJ.Akka.Projections.Storage.RavenDb;
using JetBrains.Annotations;
using Raven.Client.Documents.BulkInsert;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbProjectionStorageTests(RavenDbFixture fixture) 
    : ProjectionStorageTests<
        string, 
        RavenDbProjectionContext<RavenDbProjectionStorageTests.TestDocument>, 
        SetupRavenDbStorage>, IClassFixture<RavenDbFixture>
{
    protected override SetupRavenDbStorage GetStorage()
    {
        return new SetupRavenDbStorage(fixture.OpenDocumentStore(), new BulkInsertOptions());
    }

    protected override RavenDbProjectionContext<TestDocument> CreateTestContext(string id)
    {
        return new RavenDbProjectionContext<TestDocument>(id, new TestDocument
        {
            Id = id,
            HandledEvents = ImmutableList.Create(Guid.NewGuid())
        });
    }

    protected override RavenDbProjectionContext<TestDocument> Delete(RavenDbProjectionContext<TestDocument> context)
    {
        context.ModifyDocument(_ => null);

        return context;
    }

    protected override IProjection<string, RavenDbProjectionContext<TestDocument>, SetupRavenDbStorage> 
        CreateProjection()
    {
        return new TestProjection();
    }

    protected override Task VerifyContext(
        RavenDbProjectionContext<TestDocument> original, 
        RavenDbProjectionContext<TestDocument> loaded)
    {
        loaded.Id.Should().Be(original.Id);
        loaded.Document!.HandledEvents.Should().BeEquivalentTo(original.Document!.HandledEvents);
        
        return Task.CompletedTask;
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