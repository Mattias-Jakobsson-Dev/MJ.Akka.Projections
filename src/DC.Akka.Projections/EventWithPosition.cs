namespace DC.Akka.Projections;

public record EventWithPosition(object Event, long? Position);