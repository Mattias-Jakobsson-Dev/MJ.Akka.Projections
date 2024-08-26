using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using DC.Akka.Projections.Storage;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

public abstract class ProjectionStorageTests<TId, TDocument> : TestKit where TId : notnull where TDocument : notnull
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public virtual async Task StoreAndLoadSingleDocument()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        var original = CreateTestDocument(id);
        
        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(id, original)),
                ImmutableList<DocumentToDelete>.Empty);
        
        var document = await storage.LoadDocument<TDocument>(id);

        document.Should().NotBeNull();

        await VerifyDocument(original, document!);
    }

    [Fact]
    public virtual async Task StoreAndLoadMultipleDocuments()
    {
        var originalDocuments = Enumerable.Range(0, 5)
            .Select(_ => CreateRandomId())
            .Select(x => new
            {
                Id = x,
                Document = CreateTestDocument(x)
            })
            .ToImmutableList();
        
        var storage = GetStorage();

        await storage
            .Store(
                originalDocuments
                    .Select(x => new DocumentToStore(x.Id, x.Document))
                    .ToImmutableList(),
                ImmutableList<DocumentToDelete>.Empty);

        foreach (var originalData in originalDocuments)
        {
            var document = await storage.LoadDocument<TDocument>(originalData.Id);

            document.Should().NotBeNull();
            
            await VerifyDocument(originalData.Document, document!);
        }
    }

    [Fact]
    public virtual async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(id, CreateTestDocument(id))),
                ImmutableList.Create(new DocumentToDelete(id, typeof(TDocument))));
        
        var document = await storage.LoadDocument<TDocument>(id);

        document.Should().BeNull();
    }

    [Fact]
    public virtual async Task StoreAndDeleteSingleDocumentInTwoTransactions()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        var testDocument = CreateTestDocument(id);

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(id, testDocument)),
                ImmutableList<DocumentToDelete>.Empty);
        
        var document = await storage.LoadDocument<TDocument>(id);

        document.Should().NotBeNull();
        
        await VerifyDocument(testDocument, document!);

        await storage
            .Store(
                ImmutableList<DocumentToStore>.Empty,
                ImmutableList.Create(new DocumentToDelete(id, typeof(TDocument))));
        
        document = await storage.LoadDocument<TDocument>(id);

        document.Should().BeNull();
    }

    [Fact]
    public virtual async Task DeleteNonExistingDocument()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        await storage
            .Store(
                ImmutableList<DocumentToStore>.Empty,
                ImmutableList.Create(new DocumentToDelete(id, typeof(TDocument))));
        
        var document = await storage.LoadDocument<TDocument>(id);

        document.Should().BeNull();
    }

    [Fact]
    public virtual async Task WriteWithCancelledTask()
    {
        var storage = GetStorage();

        var id = CreateRandomId();
        
        var cancellationTokenSource = new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        await storage
            .Store(
                ImmutableList<DocumentToStore>.Empty,
                ImmutableList.Create(new DocumentToDelete(id, typeof(TDocument))), 
                cancellationTokenSource.Token)
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
        
        var document = await storage.LoadDocument<TDocument>(id, CancellationToken.None);

        document.Should().BeNull();
    }

    protected abstract IProjectionStorage GetStorage();
    
    protected abstract TDocument CreateTestDocument(TId id);
    
    protected abstract Task VerifyDocument(TDocument original, TDocument loaded);

    protected virtual TId CreateRandomId()
    {
        return _fixture.Create<TId>();
    }
}