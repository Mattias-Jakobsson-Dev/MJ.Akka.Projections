using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using FluentAssertions;
using MJ.Akka.Projections.Storage;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public abstract class ProjectionStorageTests<TId, TContext, TStorageSetup> 
    : TestKit where TId : notnull where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public virtual async Task StoreAndLoadSingleDocument()
    {
        var storageSetup = GetStorage();
        var projection = CreateProjection();
        
        var id = CreateRandomId();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();
        
        await projectionStorage.Store(CreateInsertRequest(id));

        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);

        context.Exists().Should().BeTrue();

        await VerifyContext(context);
    }

    [Fact]
    public virtual async Task StoreAndLoadMultipleDocuments()
    {
        var originalContexts = Enumerable.Range(0, 5)
            .Select(_ => CreateRandomId())
            .Select(x => new
            {
                Id = x,
                Context = CreateInsertRequest(x)
            })
            .ToImmutableList();
        
        var storageSetup = GetStorage();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        await projectionStorage
            .Store(new StoreProjectionRequest(originalContexts
                .SelectMany(x => x.Context.Results)
                .ToImmutableList()));

        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        foreach (var originalData in originalContexts)
        {
            var context = await loader.Load(originalData.Id);

            context.Exists().Should().BeTrue();
            
            await VerifyContext(context);
        }
    }

    [Fact]
    public virtual async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        var addContext = CreateInsertRequest(id);
        var deleteContext = CreateDeleteRequest(id);

        await projectionStorage
            .Store(new StoreProjectionRequest(addContext.Results.AddRange(deleteContext.Results)));
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);

        context.Exists().Should().BeFalse();
    }

    [Fact]
    public virtual async Task StoreAndDeleteSingleDocumentInTwoTransactions()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        var addContext = CreateInsertRequest(id);
        var deleteContext = CreateDeleteRequest(id);

        await projectionStorage.Store(addContext);
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);

        context.Exists().Should().BeTrue();
        
        await VerifyContext(context);

        await projectionStorage.Store(deleteContext);
        
        context = await loader.Load(id);

        context.Exists().Should().BeFalse();
    }

    [Fact]
    public virtual async Task DeleteNonExistingDocument()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        var deleteContext = CreateDeleteRequest(id);

        await projectionStorage.Store(deleteContext);
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);

        context.Exists().Should().BeFalse();
    }

    [Fact]
    public virtual async Task WriteWithCancelledTask()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        var storageSetup = GetStorage();
        var projection = CreateProjection();
        
        var id = CreateRandomId();

        var original = CreateInsertRequest(id);

        var projectionStorage = storageSetup.CreateProjectionStorage();
        
        await projectionStorage
            .Store(
                original,
                cancellationTokenSource.Token)
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));

        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id, CancellationToken.None);

        context.Exists().Should().BeFalse();
    }

    protected abstract TStorageSetup GetStorage();
    
    protected abstract StoreProjectionRequest CreateInsertRequest(TId id);

    protected abstract StoreProjectionRequest CreateDeleteRequest(TId id);
    
    protected abstract IProjection<TId, TContext, TStorageSetup> CreateProjection();
    
    protected abstract Task VerifyContext(TContext loaded);

    protected virtual TId CreateRandomId()
    {
        return _fixture.Create<TId>();
    }
}