using System.Collections.Concurrent;

namespace DC.Akka.Projections;

internal class BlockingUniqueQueue<T>
{
    private readonly ConcurrentQueue<QueueItem> _backingQueue;
    private readonly BlockingCollection<QueueItem> _queue;
    private readonly ConcurrentDictionary<object, Queue<Func<T>>> _pendingQueues = new();
    private readonly ConcurrentDictionary<object, object> _currentlyQueuedKeys = [];

    public BlockingUniqueQueue(int size)
    {
        _backingQueue = new ConcurrentQueue<QueueItem>();
        _queue = new BlockingCollection<QueueItem>(_backingQueue, size);
    }

    public bool IsEmpty => _backingQueue.IsEmpty && _pendingQueues.All(x => x.Value.Count < 1);

    public T Dequeue()
    {
        var item = _queue.Take();

        _currentlyQueuedKeys.TryRemove(item.Key, out _);

        if (!_pendingQueues.TryGetValue(item.Key, out var pendingQueue))
            return item.Item;

        if (pendingQueue.TryDequeue(out var itemToEnqueue))
        {
            Enqueue(item.Key, itemToEnqueue);
        }
        else
        {
            _pendingQueues.TryRemove(item.Key, out _);
        }

        return item.Item;
    }

    public (bool hasItems, bool hasCapacity) TryPeek(out T? item)
    {
        var success = _backingQueue.TryPeek(out var result);

        item = result != null ? result.Item : default;

        return (success, _queue.Count < _queue.BoundedCapacity);
    }

    public void Enqueue(object key, Func<T> produceItem)
    {
        if (_currentlyQueuedKeys.TryGetValue(key, out _))
        {
            var pendingQueue = _pendingQueues.GetOrAdd(key, new Queue<Func<T>>());

            pendingQueue.Enqueue(produceItem);
        }
        else
        {
            _queue.Add(new QueueItem(key, produceItem()));
        }
    }

    private record QueueItem(object Key, T Item);
}