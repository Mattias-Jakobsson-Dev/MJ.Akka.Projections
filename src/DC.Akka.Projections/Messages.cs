namespace DC.Akka.Projections;

public static class Messages
{
    public record Acknowledge : IProjectEventsResponse;

    public record Reject(Exception? Cause) : IProjectEventsResponse;

    public interface IProjectEventsResponse;
}