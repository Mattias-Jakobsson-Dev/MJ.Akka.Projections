using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public class BatchedProjectionStorage : IProjectionStorage, IRequireImmutable
{
    private readonly IProjectionStorage _innerStorage;
    private readonly ISourceQueueWithComplete<WriteQueueEntry> _writeQueue;
    private readonly int _bufferSize;

    public BatchedProjectionStorage(
        IProjectionStorage innerStorage,
        IActorRefFactory actorSystem,
        (int Number, TimeSpan Timeout) batching,
        int parallelism)
    {
        _innerStorage = innerStorage;
        _bufferSize = batching.Number * 4;

        _writeQueue = Source
            .Queue<WriteQueueEntry>(_bufferSize, OverflowStrategy.Backpressure)
            .GroupedWithin(batching.Number, batching.Timeout)
            .SelectAsync(
                parallelism,
                async items =>
                {
                    await innerStorage
                        .StoreDocuments(
                            items
                                .Select(x => (x.Document, x.Ack, x.Nack))
                                .ToImmutableList());

                    return NotUsed.Instance;
                })
            .ToMaterialized(Sink.Ignore<NotUsed>(), Keep.Left)
            .Run(actorSystem.Materializer());
    }

    public Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default)
    {
        return _innerStorage.LoadDocument<TDocument>(id, cancellationToken);
    }

    public async Task StoreDocuments(
        IImmutableList<(ProjectedDocument Document,
            Action Ack, Action<Exception?> Nack)> documents,
        CancellationToken cancellationToken = default)
    {
        foreach (var document in documents)
        {
            var result = await _writeQueue
                .OfferAsync(new WriteQueueEntry(
                    document.Document,
                    document.Ack,
                    document.Nack));

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
    }

    public Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
    {
        return _innerStorage.LoadLatestPosition(projectionName, cancellationToken);
    }

    public Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        return _innerStorage.StoreLatestPosition(projectionName, position, cancellationToken);
    }

    private record WriteQueueEntry(ProjectedDocument Document, Action Ack, Action<Exception?> Nack);
}