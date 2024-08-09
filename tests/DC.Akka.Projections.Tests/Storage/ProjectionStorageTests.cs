using System.Collections.Immutable;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

public abstract class ProjectionStorageTests<TId> : TestKit where TId : notnull
{
    [Fact]
    public async Task StoreAndLoadSingleDocument()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        var eventId = Guid.NewGuid().ToString();

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id, new TestDocument<TId>
                    {
                        Id = id,
                        HandledEvents = ImmutableList.Create(eventId)
                    })),
                ImmutableList<DocumentToDelete>.Empty);
        
        var (document, _) = await storage.LoadDocument<TestDocument<TId>>(id);

        document.Should().NotBeNull();
        document!.Id.Should().Be(id);
        document.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(eventId));
    }

    [Fact]
    public async Task StoreAndLoadMultipleDocuments()
    {
        var documentIds = Enumerable.Range(0, 5)
            .Select(_ => CreateRandomId())
            .ToImmutableList();
        
        var storage = GetStorage();
        var eventId = Guid.NewGuid().ToString();

        await storage
            .Store(
                documentIds
                    .Select(x => new DocumentToStore(
                        x,
                        new TestDocument<TId>
                        {
                            Id = x,
                            HandledEvents = ImmutableList.Create(eventId)
                        }))
                    .ToImmutableList(),
                ImmutableList<DocumentToDelete>.Empty);

        foreach (var documentId in documentIds)
        {
            var (document, _) = await storage.LoadDocument<TestDocument<TId>>(documentId);

            document.Should().NotBeNull();
            document!.Id.Should().Be(documentId);
            document.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(eventId));
        }
    }

    [Fact]
    public async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        var eventId = Guid.NewGuid().ToString();
        
        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id, new TestDocument<TId>
                    {
                        Id = id,
                        HandledEvents = ImmutableList.Create(eventId)
                    })),
                ImmutableList.Create(new DocumentToDelete(id, typeof(TestDocument<TId>))));
        
        var (document, _) = await storage.LoadDocument<TestDocument<TId>>(id);

        document.Should().BeNull();
    }

    [Fact]
    public async Task StoreAndDeleteSingleDocumentInTwoTransactions()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        var eventId = Guid.NewGuid().ToString();

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id, new TestDocument<TId>
                    {
                        Id = id,
                        HandledEvents = ImmutableList.Create(eventId)
                    })),
                ImmutableList<DocumentToDelete>.Empty);
        
        var (document, _) = await storage.LoadDocument<TestDocument<TId>>(id);

        document.Should().NotBeNull();
        document!.Id.Should().Be(id);
        document.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(eventId));

        await storage
            .Store(
                ImmutableList<DocumentToStore>.Empty,
                ImmutableList.Create(new DocumentToDelete(id, typeof(TestDocument<TId>))));
        
        (document, _) = await storage.LoadDocument<TestDocument<TId>>(id);

        document.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNonExistingDocument()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        await storage
            .Store(
                ImmutableList<DocumentToStore>.Empty,
                ImmutableList.Create(new DocumentToDelete(id, typeof(TestDocument<TId>))));
        
        var (document, _) = await storage.LoadDocument<TestDocument<TId>>(id);

        document.Should().BeNull();
    }

    protected abstract TId CreateRandomId();

    protected abstract IProjectionStorage GetStorage();
}