namespace DC.Akka.Projections;

public record EventWithPosition(object Event, long? Position);

public static class EventWithPositionExtensions
{
    public static long? GetHighestEventNumber(this IEnumerable<EventWithPosition> source)
    {
        return source.Select(x => x.Position).Max();
    }
}