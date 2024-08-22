using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public class BatchedProjectionStorage : IProjectionStorage
{
    private readonly IProjectionStorage _innerStorage;
    private readonly ISourceQueueWithComplete<IPendingWrite> _writeQueue;
    private readonly int _bufferSize;

    public BatchedProjectionStorage(
        ActorSystem actorSystem,
        IProjectionStorage innerStorage,
        int parallelism,
        IStorageBatchingStrategy batchingStrategy)
    {
        _innerStorage = innerStorage;

        _bufferSize = batchingStrategy.GetBufferSize(parallelism);

        var queue = Source
            .Queue<IPendingWrite>(_bufferSize, OverflowStrategy.Backpressure);

        queue = batchingStrategy.GetStrategy(queue);

        _writeQueue = queue
            .SelectAsync(
                parallelism,
                async write =>
                {
                    try
                    {
                        await _innerStorage.Store(write.ToUpsert, write.ToDelete);

                        write.Completed();
                    }
                    catch (Exception e)
                    {
                        write.Fail(e);
                    }

                    return NotUsed.Instance;
                })
            .ToMaterialized(Sink.Ignore<NotUsed>(), Keep.Left)
            .Run(actorSystem.Materializer());
    }

    public Task<TDocument?> LoadDocument<TDocument>(
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
                        promise.TrySetException(result.Exception ?? new Exception("Failed to write documents"));
                    }
                },
                cancellationToken: cancellationToken,
                continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
                scheduler: TaskScheduler.Default);

        return promise.Task;
    }
}