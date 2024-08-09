using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public class BatchedProjectionStorage : IProjectionStorage
{
    private readonly IProjectionStorage _innerStorage;
    private readonly ISourceQueueWithComplete<PendingWrite> _writeQueue;
    private readonly int _bufferSize;

    public BatchedProjectionStorage(
        ActorSystem actorSystem,
        IProjectionStorage innerStorage,
        int batchSize,
        int parallelism)
    {
        _innerStorage = innerStorage;

        _bufferSize = batchSize * parallelism * 2;

        _writeQueue = Source
            .Queue<PendingWrite>(_bufferSize, OverflowStrategy.Backpressure)
            .Batch(
                batchSize,
                x => x,
                (current, pending) => current.MergeWith(pending))
            .SelectAsync(
                parallelism,
                async write =>
                {
                    await _innerStorage.Store(write.ToUpsert, write.ToDelete);
                    
                    write.Completed();

                    return NotUsed.Instance;
                })
            .ToMaterialized(Sink.Ignore<NotUsed>(), Keep.Left)
            .Run(actorSystem.Materializer());
    }

    public Task<(TDocument? document, bool requireReload)> LoadDocument<TDocument>(
        object id,
        CancellationToken cancellationToken = default)
    {
        return _innerStorage.LoadDocument<TDocument>(id, cancellationToken);
    }

    public Task Store(
        IImmutableList<DocumentToStore> toUpsert,
        IImmutableList<DocumentToDelete> toDelete,
        CancellationToken cancellationToken = default)
    {
        var promise = new TaskCompletionSource<NotUsed>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        _writeQueue.OfferAsync(new PendingWrite(
            toUpsert,
            toDelete,
            promise))
            .ContinueWith(
                result =>
                {
                    if (result.IsCompletedSuccessfully)
                    {
                        switch (result.Result)
                        {
                            case QueueOfferResult.Enqueued:
                                break;

                            case QueueOfferResult.Failure f:
                                promise.TrySetException(new Exception("Failed to write documents", f.Cause));

                                break;
                            case QueueOfferResult.Dropped:
                                promise.TrySetException(new Exception(
                                    $"Failed to enqueue documents batch write, the queue buffer was full ({_bufferSize} elements)"));
                                
                                break;

                            case QueueOfferResult.QueueClosed:
                                promise.TrySetException(new Exception(
                                    "Failed to enqueue documents batch write, the queue was closed."));
                                
                                break;
                        }
                    }
                    else
                    {
                        promise.TrySetException(new Exception("Failed to write documents"));
                    }
                },
                cancellationToken: cancellationToken,
                continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
                scheduler: TaskScheduler.Default);

        return promise.Task;
    }

    private class PendingWrite
    {
        private readonly IImmutableList<TaskCompletionSource<NotUsed>> _completions;
        
        public PendingWrite(
            IImmutableList<DocumentToStore> toUpsert,
            IImmutableList<DocumentToDelete> toDelete,
            TaskCompletionSource<NotUsed> completion) : this(
            toUpsert,
            toDelete,
            ImmutableList.Create(completion))
        {
        }

        private PendingWrite(
            IImmutableList<DocumentToStore> toUpsert,
            IImmutableList<DocumentToDelete> toDelete,
            IImmutableList<TaskCompletionSource<NotUsed>> completions)
        {
            ToUpsert = toUpsert;
            ToDelete = toDelete;
            _completions = completions;
        }
        
        public IImmutableList<DocumentToStore> ToUpsert { get; }
        public IImmutableList<DocumentToDelete> ToDelete { get; }

        public PendingWrite MergeWith(PendingWrite other)
        {
            return new PendingWrite(
                Merge(ToUpsert, other.ToUpsert),
                Merge(ToDelete, other.ToDelete),
                _completions.AddRange(other._completions));
        }

        public void Completed()
        {
            foreach (var completion in _completions)
                completion.SetResult(NotUsed.Instance);
        }

        private static ImmutableList<T> Merge<T>(
            IImmutableList<T> existing,
            IImmutableList<T> newItems) where T : StorageDocument<T>
        {
            return existing
                .AddRange(newItems)
                .Aggregate(
                    ImmutableList<T>.Empty,
                    (current, item) =>
                    {
                        var existingItem = current
                            .FirstOrDefault(x => x.Equals(item));

                        return existingItem != null
                            ? current.Remove(existingItem).Add(item)
                            : current.Add(item);
                    });
        }
    }
}