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
        
        await projectionStorage.Store(new Dictionary<ProjectionContextId, IProjectionContext>
        {
            [new ProjectionContextId(projection.Name, id)] = CreateInsertRequest(id)
        }.ToImmutableDictionary());

        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().Should().BeTrue();

        await VerifyContext(context);
    }

    [Fact]
    public virtual async Task StoreAndLoadMultipleDocuments()
    {
        var projection = CreateProjection();

        var originalContexts = Enumerable.Range(0, 5)
            .Select(_ => CreateRandomId())
            .Select(x => new
            {
                Id = x,
                Context = CreateInsertRequest(x)
            })
            .ToImmutableDictionary(
                x => new ProjectionContextId(projection.Name, x.Id),
                x => (IProjectionContext)x.Context);
        
        var storageSetup = GetStorage();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        await projectionStorage
            .Store(originalContexts);

        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        foreach (var originalData in originalContexts)
        {
            var context = await loader.Load((TId)originalData.Key.ItemId, projection.GetDefaultContext);

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
            .Store(new Dictionary<ProjectionContextId, IProjectionContext>
            {
                [new ProjectionContextId(projection.Name, id)] = addContext.MergeWith(deleteContext)
            }.ToImmutableDictionary());
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id, projection.GetDefaultContext);

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

        await projectionStorage.Store(new Dictionary<ProjectionContextId, IProjectionContext>
        {
            [new ProjectionContextId(projection.Name, id)] = addContext
        }.ToImmutableDictionary());
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().Should().BeTrue();
        
        await VerifyContext(context);

        await projectionStorage.Store(new Dictionary<ProjectionContextId, IProjectionContext>
        {
            [new ProjectionContextId(projection.Name, id)] = deleteContext
        }.ToImmutableDictionary());
        
        context = await loader.Load(id, projection.GetDefaultContext);

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

        await projectionStorage.Store(new Dictionary<ProjectionContextId, IProjectionContext>
        {
            [new ProjectionContextId(projection.Name, id)] = deleteContext
        }.ToImmutableDictionary());
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id, projection.GetDefaultContext);

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
                new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(projection.Name, id)] = original
                }.ToImmutableDictionary(),
                cancellationTokenSource.Token)
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));

        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id, projection.GetDefaultContext, CancellationToken.None);

        context.Exists().Should().BeFalse();
    }

    protected abstract TStorageSetup GetStorage();
    
    protected abstract TContext CreateInsertRequest(TId id);

    protected abstract TContext CreateDeleteRequest(TId id);
    
    protected abstract IProjection<TId, TContext, TStorageSetup> CreateProjection();
    
    protected abstract Task VerifyContext(TContext loaded);

    protected virtual TId CreateRandomId()
    {
        return _fixture.Create<TId>();
    }
}