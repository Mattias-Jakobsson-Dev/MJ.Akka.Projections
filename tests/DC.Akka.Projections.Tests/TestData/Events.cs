using System.Collections.Immutable;

namespace DC.Akka.Projections.Tests.TestData;

public static class Events<TId>
{
    public record FirstEvent(TId DocId, string EventId) : IEvent;

    public record SecondEvent(TId DocId, string EventId) : IEvent;

    public record UnHandledEvent(TId DocId);

    public record TransformToMultipleEvents(IImmutableList<IEvent> Events);

    public record FailProjection(
        TId DocId,
        string EventId,
        string FailureKey,
        int ConsecutiveFailures,
        Exception FailWith);

    public interface IEvent
    {
        TId DocId { get; }
        string EventId { get; }
    }
}
