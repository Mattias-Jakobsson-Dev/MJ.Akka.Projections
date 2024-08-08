using System.Collections.Immutable;
using Akka.Actor;
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
        var probe = CreateTestProbe();
        
        var storage = GetStorage();

        var id = CreateRandomId();

        var eventId = Guid.NewGuid().ToString();
        
        var transaction = await storage.StartTransaction(
            ImmutableList.Create(
                (id, new TestDocument<TId>
                {
                    Id = id,
                    HandledEvents = ImmutableList.Create(eventId)
                }, probe.Ref)),
            ImmutableList<(TId id, IActorRef ackTo)>.Empty);

        await transaction.Commit();

        await probe.ExpectMsgAsync<Messages.Acknowledge>();

        var (document, _) = await storage.LoadDocument(id);

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
        
        var probe = CreateTestProbe();
        
        var storage = GetStorage();
        var eventId = Guid.NewGuid().ToString();
        
        var transaction = await storage.StartTransaction(
            documentIds
                .Select(x => (x, new TestDocument<TId>
                {
                    Id = x,
                    HandledEvents = ImmutableList.Create(eventId)
                }, probe.Ref))
                .ToImmutableList(),
            ImmutableList<(TId id, IActorRef ackTo)>.Empty);

        await transaction.Commit();

        foreach (var _ in documentIds)
        {
            await probe.ExpectMsgAsync<Messages.Acknowledge>();   
        }

        foreach (var documentId in documentIds)
        {
            var (document, _) = await storage.LoadDocument(documentId);

            document.Should().NotBeNull();
            document!.Id.Should().Be(documentId);
            document.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(eventId));   
        }
    }

    [Fact]
    public async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var probe = CreateTestProbe();
        
        var storage = GetStorage();

        var id = CreateRandomId();

        var eventId = Guid.NewGuid().ToString();
        
        var transaction = await storage.StartTransaction(
            ImmutableList.Create(
                (id, new TestDocument<TId>
                {
                    Id = id,
                    HandledEvents = ImmutableList.Create(eventId)
                }, probe.Ref)),
            ImmutableList.Create((id, probe.Ref)));

        await transaction.Commit();

        await probe.ExpectMsgAsync<Messages.Acknowledge>();
        await probe.ExpectMsgAsync<Messages.Acknowledge>();

        var (document, _) = await storage.LoadDocument(id);

        document.Should().BeNull();
    }
    
    [Fact]
    public async Task StoreAndDeleteSingleDocumentInTwoTransactions()
    {
        var probe = CreateTestProbe();
        
        var storage = GetStorage();

        var id = CreateRandomId();

        var eventId = Guid.NewGuid().ToString();
        
        var storeTransaction = await storage.StartTransaction(
            ImmutableList.Create(
                (id, new TestDocument<TId>
                {
                    Id = id,
                    HandledEvents = ImmutableList.Create(eventId)
                }, probe.Ref)),
            ImmutableList<(TId id, IActorRef ackTo)>.Empty);

        await storeTransaction.Commit();

        await probe.ExpectMsgAsync<Messages.Acknowledge>();

        var (document, _) = await storage.LoadDocument(id);

        document.Should().NotBeNull();
        document!.Id.Should().Be(id);
        document.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(eventId));
        
        var deleteTransaction = await storage.StartTransaction(
            ImmutableList<(TId Id, TestDocument<TId> Document, IActorRef ackTo)>.Empty, 
            ImmutableList.Create((id, probe.Ref)));

        await deleteTransaction.Commit();

        await probe.ExpectMsgAsync<Messages.Acknowledge>();

        (document, _) = await storage.LoadDocument(id);

        document.Should().BeNull();
    }

    [Fact]
    public async Task DeleteNonExistingDocument()
    {
        var probe = CreateTestProbe();
        
        var storage = GetStorage();

        var id = CreateRandomId();
        
        var deleteTransaction = await storage.StartTransaction(
            ImmutableList<(TId Id, TestDocument<TId> Document, IActorRef ackTo)>.Empty, 
            ImmutableList.Create((id, probe.Ref)));

        await deleteTransaction.Commit();

        await probe.ExpectMsgAsync<Messages.Acknowledge>();

        var (document, _) = await storage.LoadDocument(id);

        document.Should().BeNull();
    }

    protected abstract TId CreateRandomId();
    
    protected abstract IProjectionStorage<TId, TestDocument<TId>> GetStorage();
}