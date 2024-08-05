using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DC.Akka.Projections.Tests.TestData;

public static class Events
{
    public record FirstEvent(string DocId) : IEvent;

    public record SecondEvent(string DocId) : IEvent;

    public record TransformToMultipleEvents(IImmutableList<IEvent> Events);

    [JsonDerivedType(typeof(FirstEvent), "FirstEvent")]
    [JsonDerivedType(typeof(SecondEvent), "SecondEvent")]
    public interface IEvent
    {
        string DocId { get; }
    }
}