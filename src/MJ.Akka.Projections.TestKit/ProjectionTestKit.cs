using System.Collections.Concurrent;
using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage;
using Xunit;

namespace MJ.Akka.Projections.TestKit;

[PublicAPI]
public abstract class ProjectionTestKit<TId, TContext, TStorageSetup> 
    : global::Akka.TestKit.Xunit2.TestKit, IAsyncLifetime
    where TId : notnull where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    private ILoadProjectionContext<TId, TContext> _contextLoader = null!;
    private TestProjection<TId, TContext, TStorageSetup> _projection = null!;

    private readonly ConcurrentDictionary<ProjectionContextId, IProjectionContext> _storage = new();
    
    protected virtual TimeSpan Timeout => TimeSpan.FromSeconds(10);
    protected abstract IProjection<TId, TContext, TStorageSetup> GetProjectionToTest();
    protected virtual Task Setup() => Task.CompletedTask;
    protected virtual IImmutableDictionary<TId, TContext> Given() => ImmutableDictionary<TId, TContext>.Empty;
    protected abstract IEnumerable<object> When();
    protected virtual Task Then() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        await Setup();
        
        _projection = new TestProjection<TId, TContext, TStorageSetup>(GetProjectionToTest(), When().ToArray());

        foreach (var context in Given())
        {
            _storage[new ProjectionContextId(_projection.Name, context.Key)] = context.Value;
        }

        var storageSetup = new ProjectionTestStorageSetup(_storage);
        
        var coordinator = await Sys
            .Projections(config => config
                        .WithProjection(_projection)
                    .WithEventBatchingStrategy(new NoEventBatchingStrategy(100))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy()),
                storageSetup)
            .Start();
        
        var proxy = coordinator.Get(_projection.Name)!;
        
        await proxy.WaitForCompletion(Timeout);

        _contextLoader = _projection.GetLoadProjectionContext(storageSetup);

        await Then();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<TContext> LoadContext(TId id)
    {
        return await _contextLoader.Load(id, _projection.GetDefaultContext);
    }

    public IImmutableDictionary<TId, TContext> GetStoredContexts()
    {
        return _storage
            .ToImmutableDictionary(
                x => (TId)x.Key.ItemId,
                x => (TContext)x.Value);
    }
}