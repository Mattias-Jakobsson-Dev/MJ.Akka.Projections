using System.Collections.Immutable;

namespace MJ.Akka.Projections.Tests.TestData;

public static class Events<TId>
{
    public record FirstEvent(TId DocId, string EventId) : IEvent;
    
    public record UnHandledEvent(TId DocId);

    public record EventWithFilter(TId DocId, string EventId, Func<bool> Filter) : IEvent;

    public record DelayHandlingWithoutCancellationToken(TId DocId, string EventId, TimeSpan Delay) : IEvent;

    public record DelayHandlingWithCancellationToken(TId DocId, string EventId, TimeSpan Delay) : IEvent;
    
    public record TransformToMultipleEvents(IImmutableList<IEvent> Events);

    public record FailProjection(
        TId DocId,
        string EventId,
        string FailureKey,
        int ConsecutiveFailures,
        Exception FailWith) : IEvent;

    public interface IEvent
    {
        TId DocId { get; }
        string EventId { get; }
    }
}
