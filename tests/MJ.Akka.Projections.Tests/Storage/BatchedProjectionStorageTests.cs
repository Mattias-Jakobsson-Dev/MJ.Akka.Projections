using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using MJ.Akka.Projections.Storage;
using FluentAssertions;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Storage.Messages;
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

        var writes = Enumerable
            .Range(0, 10)
            .Select(_ =>
            {
                var id = _fixture.Create<string>();
                
                return new DocumentResults.DocumentModified(
                    id,
                    new TestDocument<string>
                    {
                        Id = id
                    });
            })
            .OfType<IProjectionResult>()
            .ToImmutableList();

        await Task.WhenAll(
            writes
                .Select(write => batchedStorage.Store(new StoreProjectionRequest(ImmutableList.Create(write)))));

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

        var writes = Enumerable
            .Range(0, 10)
            .Select(_ =>
            {
                var id = _fixture.Create<string>();
                
                return new DocumentResults.DocumentModified(
                    id,
                    new TestDocument<string>
                    {
                        Id = id
                    });
            })
            .OfType<IProjectionResult>()
            .ToImmutableList();

        await Task.WhenAll(
            writes
                .Select(write => batchedStorage.Store(new StoreProjectionRequest(ImmutableList.Create(write)))));

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

        var task = batchedStorage
            .Store(
                new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
                    new DocumentResults.DocumentModified(id, new TestDocument<string>
                    {
                        Id = id
                    }))),
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

        await cancellationTokenSource.CancelAsync();
        
        var firstTask = batchedStorage
            .Store(
                new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
                    new DocumentResults.DocumentModified(firstId, new TestDocument<string>
                    {
                        Id = firstId
                    }))),
                cancellationTokenSource.Token);
        
        var secondTask = batchedStorage
            .Store(
                new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
                    new DocumentResults.DocumentModified(secondId, new TestDocument<string>
                    {
                        Id = secondId
                    }))),
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

        var loader = new InMemoryProjectionLoader<string, TestDocument<string>>(id => setup.LoadDocument(id));
        
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
                new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
                    new DocumentResults.DocumentModified(firstId, new TestDocument<string>
                    {
                        Id = firstId
                    }))),
                firstCancellationTokenSource.Token);
        
        var secondTask = batchedStorage
            .Store(
                new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
                    new DocumentResults.DocumentModified(secondId, new TestDocument<string>
                    {
                        Id = secondId
                    }))),
                secondCancellationTokenSource.Token);
        
        await firstTask
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));

        await secondTask
            .ShouldCompleteWithin(TimeSpan.FromSeconds(1));
        
        var document = await loader.Load(secondId, secondCancellationTokenSource.Token);

        document.Exists().Should().BeTrue();
        document.Id.Should().Be(secondId);
    }
    
    private class StorageWithDelay(TimeSpan delay) : IProjectionStorage
    {
        private readonly object _lock = new { };
        
        private readonly InMemoryProjectionStorage _storage = new(new ConcurrentDictionary<object, ReadOnlyMemory<byte>>());
        public int NumberOfWrites { get; private set; }
        
        public async Task<StoreProjectionResponse> Store(
            StoreProjectionRequest request, 
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);

            var response = await _storage.Store(request, cancellationToken);

            lock (_lock)
            {
                NumberOfWrites++;
            }

            return response;
        }
    }
}