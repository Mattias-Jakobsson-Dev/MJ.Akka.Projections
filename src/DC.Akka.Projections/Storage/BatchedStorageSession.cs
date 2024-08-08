using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections.Storage;

public class BatchedStorageSession : IStorageSession
{
    private readonly ProjectionsApplication _application;
    private readonly ISourceQueueWithComplete<IStorageTransaction> _writeQueue;
    private readonly int _bufferSize;

    public BatchedStorageSession(
        ProjectionsApplication application,
        int batchSize,
        int parallelism)
    {
        _application = application;
        _bufferSize = batchSize * parallelism * 2;

        _writeQueue = Source
            .Queue<IStorageTransaction>(_bufferSize, OverflowStrategy.Backpressure)
            .Batch(
                batchSize,
                x => x,
                (current, pending) => current.MergeWith(
                    pending,
                    (mergeWith, item) => mergeWith.MergeWith(item, MergedTransactions.Create)))
            .SelectAsync(
                parallelism,
                async transaction =>
                {
                    await transaction.Commit();

                    return NotUsed.Instance;
                })
            .ToMaterialized(Sink.Ignore<NotUsed>(), Keep.Left)
            .Run(application.ActorSystem.Materializer());
    }

    public async Task Store<TId, TDocument>(
        string projectionName,
        TId id,
        TDocument document,
        IActorRef ackTo,
        CancellationToken cancellationToken = default) where TId : notnull where TDocument : notnull
    {
        var storage = _application
                          .GetProjectionConfiguration<TId, TDocument>(projectionName) ??
                      throw new NoDocumentProjectionException<TId, TDocument>(id);

        var transaction = await storage.DocumentStorage.StartTransaction(
            ImmutableList.Create<(TId Id, TDocument Document, IActorRef ackTo)>(
                (id, document, ackTo)),
            ImmutableArray<(TId id, IActorRef ackTo)>.Empty,
            cancellationToken);

        await EnqueueTransaction(transaction);
    }

    public async Task Delete<TId, TDocument>(
        string projectionName,
        TId id,
        IActorRef ackTo,
        CancellationToken cancellationToken = default)  where TId : notnull where TDocument : notnull
    {
        var storage = _application
                          .GetProjectionConfiguration<TId, TDocument>(projectionName) ??
                      throw new NoDocumentProjectionException<TId, TDocument>(id);

        var transaction = await storage.DocumentStorage.StartTransaction(
            ImmutableList<(TId Id, TDocument Document, IActorRef ackTo)>.Empty,
            ImmutableList.Create(
                (id, ackTo)),
            cancellationToken);

        await EnqueueTransaction(transaction);
    }

    private async Task EnqueueTransaction(IStorageTransaction transaction)
    {
        var result = await _writeQueue.OfferAsync(transaction);

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