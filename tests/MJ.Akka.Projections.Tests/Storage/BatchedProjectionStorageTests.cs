using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.TestKit.Xunit2;
using AutoFixture;
using MJ.Akka.Projections.Storage;
using Shouldly;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public class BatchedProjectionStorageTests : global::Akka.TestKit.Xunit2.TestKit
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public async Task Ensure_multiple_concurrent_writes_with_batch_size_strategy()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromMilliseconds(100));

        var batchedStorage = new BatchedProjectionStorage(
            Sys,
            innerStorage,
            1,
            new BatchSizeStorageBatchingStrategy(5));

        var projectionName = _fixture.Create<string>();

        var writes = Enumerable
            .Range(0, 10)
            .Select(_ =>
            {
                var id = _fixture.Create<string>();
                
                return new InMemoryProjectionContext<string, TestDocument<string>>(
                    id,
                    new TestDocument<string>
                    {
                        Id = id
                    });
            })
            .ToImmutableList();

        await Task.WhenAll(
            writes
                .Select(write => batchedStorage
                    .Store(new Dictionary<ProjectionContextId, IProjectionContext>
                    {
                        [new ProjectionContextId(projectionName, write.Id)] = write
                    }.ToImmutableDictionary())));

        innerStorage.NumberOfWrites.ShouldBeLessThan(4);
    }
    
    [Fact]
    public async Task Ensure_multiple_concurrent_writes_with_batch_within_strategy()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromMilliseconds(100));

        var batchedStorage = new BatchedProjectionStorage(
            Sys,
            innerStorage,
            1,
            new BufferWithinStorageBatchingStrategy(10, TimeSpan.FromMilliseconds(200)));

        var projectionName = _fixture.Create<string>();

        var writes = Enumerable
            .Range(0, 10)
            .Select(_ =>
            {
                var id = _fixture.Create<string>();
                
                return new InMemoryProjectionContext<string, TestDocument<string>>(
                    id,
                    new TestDocument<string>
                    {
                        Id = id
                    });
            })
            .ToImmutableList();

        await Task.WhenAll(
            writes
                .Select(write => batchedStorage
                    .Store(new Dictionary<ProjectionContextId, IProjectionContext>
                    {
                        [new ProjectionContextId(projectionName, write.Id)] = write
                    }.ToImmutableDictionary())));

        innerStorage.NumberOfWrites.ShouldBe(1);
    }

    [Fact]
    public async Task Ensure_cancellation_token_cancels_write()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromSeconds(10));
        
        var batchedStorage = new BatchedProjectionStorage(
            Sys,
            innerStorage,
            1,
            new NoStorageBatchingStrategy());

        var cancellationTokenSource = new CancellationTokenSource();
        
        SimpleIdContext<string> id = _fixture.Create<string>();
        var projectionName = _fixture.Create<string>();

        var task = batchedStorage
            .Store(
                new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(projectionName, id)] =
                        new InMemoryProjectionContext<string, TestDocument<string>>(
                            id,
                            new TestDocument<string>
                            {
                                Id = id
                            })
                }.ToImmutableDictionary(),
                cancellationTokenSource.Token);
        
        await cancellationTokenSource.CancelAsync();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }
    
    [Fact]
    public async Task Ensure_cancellation_token_cancels_two_writes_with_same_token()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromSeconds(10));
        
        var batchedStorage = new BatchedProjectionStorage(
            Sys,
            innerStorage,
            1,
            new NoStorageBatchingStrategy());
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        SimpleIdContext<string> firstId = _fixture.Create<string>();
        SimpleIdContext<string> secondId = _fixture.Create<string>();
        var projectionName = _fixture.Create<string>();

        await cancellationTokenSource.CancelAsync();
        
        var firstTask = batchedStorage
            .Store(
                new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(projectionName, firstId)] =
                        new InMemoryProjectionContext<string, TestDocument<string>>(
                            firstId,
                            new TestDocument<string>
                            {
                                Id = firstId
                            })
                }.ToImmutableDictionary(),
                cancellationTokenSource.Token);
        
        var secondTask = batchedStorage
            .Store(
                new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(projectionName, secondId)] =
                        new InMemoryProjectionContext<string, TestDocument<string>>(
                            secondId,
                            new TestDocument<string>
                            {
                                Id = secondId
                            })
                }.ToImmutableDictionary(),
                cancellationTokenSource.Token);
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => firstTask);
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => secondTask);
    }
    
    [Fact]
    public async Task Ensure_cancellation_token_cancels_correct_write()
    {
        var setup = new SetupInMemoryStorage();
        
        var innerStorage = setup.CreateProjectionStorage();
        
        var projectionName = _fixture.Create<string>();

        var loader = new InMemoryProjectionLoader<string, TestDocument<string>>(
            id => setup.LoadDocument(new ProjectionContextId(projectionName, id)));
        
        var batchedStorage = new BatchedProjectionStorage(
            Sys,
            innerStorage,
            1,
            new NoStorageBatchingStrategy());
        
        var firstCancellationTokenSource = new CancellationTokenSource();
        var secondCancellationTokenSource = new CancellationTokenSource();
        
        SimpleIdContext<string> firstId = _fixture.Create<string>();
        SimpleIdContext<string> secondId = _fixture.Create<string>();

        await firstCancellationTokenSource.CancelAsync();
        
        var firstTask = batchedStorage
            .Store(
                new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(projectionName, firstId)] =
                        new InMemoryProjectionContext<string, TestDocument<string>>(
                            firstId,
                            new TestDocument<string>
                            {
                                Id = firstId
                            })
                }.ToImmutableDictionary(),
                firstCancellationTokenSource.Token);
        
        var secondTask = batchedStorage
            .Store(
                new Dictionary<ProjectionContextId, IProjectionContext>
                {
                    [new ProjectionContextId(projectionName, secondId)] =
                        new InMemoryProjectionContext<string, TestDocument<string>>(
                            secondId,
                            new TestDocument<string>
                            {
                                Id = secondId
                            })
                }.ToImmutableDictionary(),
                secondCancellationTokenSource.Token);
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => firstTask);

        await secondTask;
        
        var document = await loader.Load(
            secondId,
            id => new InMemoryProjectionContext<string, TestDocument<string>>(id, null),
            secondCancellationTokenSource.Token);

        document.Exists().ShouldBeTrue();
        document.Id.ShouldBe(secondId);
    }
    
    private class StorageWithDelay(TimeSpan delay) : IProjectionStorage
    {
        private readonly object _lock = new { };
        
        private readonly InMemoryProjectionStorage _storage = new(new ConcurrentDictionary<ProjectionContextId, ReadOnlyMemory<byte>>());
        public int NumberOfWrites { get; private set; }
        
        public async Task Store(
            IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts, 
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);

            await _storage.Store(contexts, cancellationToken);

            lock (_lock)
            {
                NumberOfWrites++;
            }
        }
    }
}