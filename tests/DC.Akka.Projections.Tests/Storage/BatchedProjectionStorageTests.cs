using System.Collections.Immutable;
using Akka.TestKit.Xunit2;
using AutoFixture;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

public class BatchedProjectionStorageTests : TestKit
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public async Task Ensure_multiple_concurrent_writes_with_batch_size_strategy()
    {
        var innerStorage = new StorageWithDelay();

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
        var innerStorage = new StorageWithDelay();

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
    
    private class StorageWithDelay : IProjectionStorage
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
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

            await _storage.Store(toUpsert, toDelete, cancellationToken);

            lock (_lock)
            {
                NumberOfWrites++;
            }
        }
    }
}