using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using MJ.Akka.Projections.Storage;
using FluentAssertions;
using MJ.Akka.Projections.Storage;
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

        var batchedStorage = innerStorage
            .Batched(Sys, 1, new BatchSizeStorageBatchingStrategy(5));

        var writes = Enumerable
            .Range(0, 10)
            .Select(_ => new TestDocument<string>
            {
                Id = _fixture.Create<string>()
            })
            .Select(doc => ImmutableList.Create(new DocumentToStore(doc.Id, doc)))
            .ToImmutableList();

        await Task.WhenAll(
            writes
                .Select(write => batchedStorage.Store(write, ImmutableList<DocumentToDelete>.Empty)));

        innerStorage.NumberOfWrites.Should().BeLessThan(4);
    }
    
    [Fact]
    public async Task Ensure_multiple_concurrent_writes_with_batch_within_strategy()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromMilliseconds(100));

        var batchedStorage = innerStorage
            .Batched(Sys, 1, new BufferWithinStorageBatchingStrategy(10, TimeSpan.FromMilliseconds(200)));

        var writes = Enumerable
            .Range(0, 10)
            .Select(_ => new TestDocument<string>
            {
                Id = _fixture.Create<string>()
            })
            .Select(doc => ImmutableList.Create(new DocumentToStore(doc.Id, doc)))
            .ToImmutableList();

        await Task.WhenAll(
            writes
                .Select(write => batchedStorage.Store(write, ImmutableList<DocumentToDelete>.Empty)));

        innerStorage.NumberOfWrites.Should().Be(1);
    }

    [Fact]
    public async Task Ensure_cancellation_token_cancels_write()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromSeconds(10));
        
        var batchedStorage = innerStorage
            .Batched(Sys, 1, new NoStorageBatchingStrategy());
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        var id = _fixture.Create<string>();

        var task = batchedStorage
            .Store(
                ImmutableList.Create(new DocumentToStore(id, new TestDocument<string>
                {
                    Id = id
                })),
                ImmutableList<DocumentToDelete>.Empty,
                cancellationTokenSource.Token);
        
        await cancellationTokenSource.CancelAsync();
        
        await task
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public async Task Ensure_cancellation_token_cancels_two_writes()
    {
        var innerStorage = new StorageWithDelay(TimeSpan.FromSeconds(10));
        
        var batchedStorage = innerStorage
            .Batched(Sys, 1, new NoStorageBatchingStrategy());
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        var firstId = _fixture.Create<string>();
        var secondId = _fixture.Create<string>();

        var firstTask = batchedStorage
            .Store(
                ImmutableList.Create(new DocumentToStore(firstId, new TestDocument<string>
                {
                    Id = firstId
                })),
                ImmutableList<DocumentToDelete>.Empty,
                cancellationTokenSource.Token);
        
        var secondTask = batchedStorage
            .Store(
                ImmutableList.Create(new DocumentToStore(secondId, new TestDocument<string>
                {
                    Id = secondId
                })),
                ImmutableList<DocumentToDelete>.Empty,
                cancellationTokenSource.Token);
        
        await cancellationTokenSource.CancelAsync();
        
        await firstTask
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
        
        await secondTask
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
    }
    
    private class StorageWithDelay(TimeSpan delay) : IProjectionStorage
    {
        private readonly object _lock = new { };
        
        private readonly InMemoryProjectionStorage _storage = new();
        public int NumberOfWrites { get; private set; }
        
        public Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default)
        {
            return _storage.LoadDocument<TDocument>(id, cancellationToken);
        }

        public async Task Store(
            IImmutableList<DocumentToStore> toUpsert, 
            IImmutableList<DocumentToDelete> toDelete,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);

            await _storage.Store(toUpsert, toDelete, cancellationToken);

            lock (_lock)
            {
                NumberOfWrites++;
            }
        }
    }
}