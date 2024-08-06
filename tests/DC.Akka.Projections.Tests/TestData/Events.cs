using System.Collections.Immutable;

namespace DC.Akka.Projections.Tests.TestData;

public static class Events<TId>
{
    public record FirstEvent(TId DocId, string EventId) : IEvent;

    public record SecondEvent(TId DocId, string EventId) : IEvent;

    public record UnHandledEvent(TId DocId);

    public record TransformToMultipleEvents(IImmutableList<IEvent> Events);
    
    public interface IEvent;
}
