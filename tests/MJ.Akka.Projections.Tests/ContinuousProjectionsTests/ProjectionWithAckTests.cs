using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionWithAckTests(NormalTestKitActorSystem actorSystemSetup) 
    : TestProjectionBaseContinuousTests<string>(actorSystemSetup), IClassFixture<NormalTestKitActorSystem>
{
    protected override TimeSpan Timeout => TimeSpan.FromSeconds(20);

    protected override IProjection<
        SimpleIdContext<string>,
        InMemoryProjectionContext<SimpleIdContext<string>, TestDocument<string>>,
        SetupInMemoryStorage> GetProjection(
        IImmutableList<object> events,
        IImmutableList<StorageFailures> storageFailures,
        long? initialPosition = null)
    {
        return new TestProjection<string>(
            new AckGatedEnumerable(
                events.Select((evnt, index) => new EventWithPosition(evnt, index + 1))),
            storageFailures,
            initialPosition: initialPosition);
    }

    private class AckGatedEnumerable(IEnumerable<EventWithPosition> source)
        : IAsyncEnumerable<EventWithPosition>
    {
        public IAsyncEnumerator<EventWithPosition> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AckGatedAsyncEnumerator(source.GetEnumerator(), cancellationToken);
    }

    private class AckGatedAsyncEnumerator(
        IEnumerator<EventWithPosition> source,
        CancellationToken cancellationToken) : IAsyncEnumerator<EventWithPosition>
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public EventWithPosition Current { get; private set; } = null!;

        public async ValueTask<bool> MoveNextAsync()
        {
            await _gate.WaitAsync(cancellationToken);

            if (!source.MoveNext())
                return false;

            Current = new GatedAckEvent(source.Current, _gate);

            return true;
        }

        public ValueTask DisposeAsync()
        {
            source.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private record GatedAckEvent(object Event, long? Position, SemaphoreSlim Gate)
        : EventWithPosition(Event, Position), IEventWithAck
    {
        public GatedAckEvent(EventWithPosition inner, SemaphoreSlim gate)
            : this(inner.Event, inner.Position, gate) { }

        public Task Ack()
        {
            Gate.Release();
            
            return Task.CompletedTask;
        }

        public Task Nack(Exception? exception = null)
        {
            Gate.Release();
            
            return Task.CompletedTask;
        }
    }
}