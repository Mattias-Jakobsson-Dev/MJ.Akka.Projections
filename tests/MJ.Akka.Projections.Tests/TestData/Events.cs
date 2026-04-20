using System.Collections.Immutable;

namespace MJ.Akka.Projections.Tests.TestData;

public static class Events<TId>
{
    public record FirstEvent(TId DocId, string EventId) : IEvent;

    public record UnHandledEvent(TId DocId);

    public record EventWithFilter(TId DocId, string EventId, Func<bool> Filter) : IEvent;

    public record EventThatDoesntGetDocumentId(TId DocId, string EventId) : IEvent;

    public record DelayHandlingWithoutCancellationToken(TId DocId, string EventId, TimeSpan Delay) : IEvent;

    public record DelayHandlingWithCancellationToken(TId DocId, string EventId, TimeSpan Delay) : IEvent;
    
    public record TransformToMultipleEvents(IImmutableList<IEvent> Events);

    /// <summary>Event where data is used to determine the document id (WithData + WithId).</summary>
    public record EventWithDataId(TId DocId, string EventId, string Data) : IEvent;

    /// <summary>Event where data is passed to the handler (WithData + WithId + HandleWith).</summary>
    public record EventWithDataHandler(TId DocId, string EventId, string Data) : IEvent;

    /// <summary>Event where data is used in the transform function (WithData + Transform).</summary>
    public record EventWithDataTransform(TId DocId, string EventId, string Data, IImmutableList<IEvent> TransformTo) : IEvent;

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
