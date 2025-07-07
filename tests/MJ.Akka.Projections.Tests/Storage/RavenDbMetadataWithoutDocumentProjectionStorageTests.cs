using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using Akka.TestKit.Extensions;
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
public class RavenDbMetadataWithoutDocumentProjectionStorageTests(RavenDbFixture fixture) 
    : ProjectionStorageTests<
        string, 
        RavenDbProjectionContext<RavenDbMetadataWithoutDocumentProjectionStorageTests.TestDocument>, 
        SetupRavenDbStorage>, IClassFixture<RavenDbFixture>
{
    private readonly IDocumentStore _documentStore = fixture.OpenDocumentStore();
    private readonly DateTime _now = DateTime.Now;
    private readonly Guid _eventId = Guid.NewGuid();

    public override async Task WriteWithCancelledTask()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        var storageSetup = GetStorage();
        
        var id = CreateRandomId();

        var original = CreateInsertRequest(id);

        var projectionStorage = storageSetup.CreateProjectionStorage();
        
        await projectionStorage
            .Store(
                original,
                cancellationTokenSource.Token)
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));

        using var session = _documentStore.OpenAsyncSession();
        
        var document = await session.LoadAsync<TestDocument>(id, CancellationToken.None);

        var metadata = session.Advanced.GetMetadataFor(document);
        
        metadata.Keys.Should().NotContain("testkey");
    }

    protected override SetupRavenDbStorage GetStorage()
    {
        return new SetupRavenDbStorage(_documentStore, new BulkInsertOptions());
    }

    protected override StoreProjectionRequest CreateInsertRequest(string id)
    {
        using var session = _documentStore.OpenSession();
        
        session.Store(new TestDocument
        {
            Id = id,
            HandledEvents = ImmutableList.Create(_eventId)
        });
        
        session.SaveChanges();
        
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
            new StoreMetadata(id, "testkey", "testvalue")));
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

        var metadata = session.Advanced.GetMetadataFor(document);

        metadata["testkey"].Should().Be("testvalue");
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