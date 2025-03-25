namespace MJ.Akka.Projections;

public static class Messages
{
    public record Acknowledge(long? Position) : IProjectEventsResponse;

    public record Reject(Exception? Cause) : IProjectEventsResponse;

    public interface IProjectEventsResponse;
}