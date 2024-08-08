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

    public async Task Store(
        IImmutableList<DocumentToStore> toUpsert, 
        IImmutableList<DocumentToDelete> toDelete, 
        CancellationToken cancellationToken = default)
    {
        var result = await _writeQueue.OfferAsync(new PendingWrite(
            toUpsert,
            toDelete));

        switch (result)
        {
            case QueueOfferResult.Enqueued:
                break;

            case QueueOfferResult.Failure f:
                throw new Exception("Failed to write documents", f.Cause);

            case QueueOfferResult.Dropped:
                throw new Exception(
                    $"Failed to enqueue documents batch write, the queue buffer was full ({_bufferSize} elements)");

            case QueueOfferResult.QueueClosed:
                throw new Exception(
                    "Failed to enqueue documents batch write, the queue was closed.");
        }
    }

    private record PendingWrite(
        IImmutableList<DocumentToStore> ToUpsert,
        IImmutableList<DocumentToDelete> ToDelete)
    {
        public PendingWrite MergeWith(PendingWrite other)
        {
            return new PendingWrite(
                ToUpsert.AddRange(other.ToUpsert),
                ToDelete.AddRange(other.ToDelete));
        }
    }
}