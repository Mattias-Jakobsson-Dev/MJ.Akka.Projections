namespace MJ.Akka.Projections.EventStoreToRavenDbExample;

public static class Events
{
    public record FirstEvent(string Slug, string EventId, string TestData);

    public record SecondEvent(string Slug, string EventId, int TestData);

    public record ThirdEvent(
        string Slug,
        string StringEventId,
        string IntEventId,
        string StringTestData,
        int IntTestData);
}