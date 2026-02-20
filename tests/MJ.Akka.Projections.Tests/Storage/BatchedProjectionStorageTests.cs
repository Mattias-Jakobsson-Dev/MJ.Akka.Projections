using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using MJ.Akka.Projections.Storage;
using FluentAssertions;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public class BatchedProjectionStorageTests : TestKit
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

        innerStorage.NumberOfWrites.Should().BeLessThan(4);
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

        innerStorage.NumberOfWrites.Should().Be(1);
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
        
        var id = _fixture.Create<string>();
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
        
        await task
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
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
        
        var firstId = _fixture.Create<string>();
        var secondId = _fixture.Create<string>();
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
        
        await firstTask
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
        
        await secondTask
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
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
        
        var firstId = _fixture.Create<string>();
        var secondId = _fixture.Create<string>();

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
        
        await firstTask
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));

        await secondTask
            .ShouldCompleteWithin(TimeSpan.FromSeconds(1));
        
        var document = await loader.Load(
            secondId,
            id => new InMemoryProjectionContext<string, TestDocument<string>>(id, null),
            secondCancellationTokenSource.Token);

        document.Exists().Should().BeTrue();
        document.Id.Should().Be(secondId);
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