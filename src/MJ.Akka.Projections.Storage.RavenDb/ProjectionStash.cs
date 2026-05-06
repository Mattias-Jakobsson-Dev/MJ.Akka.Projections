using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public class ProjectionStash
{
    public ProjectionStash()
    {
    }

    public ProjectionStash(ProjectionContextId id)
    {
        Id = BuildId(id);
        ProjectionName = id.ProjectionName;
        DocumentId = id.ItemId.GetStringRepresentation();
    }

    public string Id { get; set; } = null!;
    public string ProjectionName { get; set; } = null!;
    public string DocumentId { get; set; } = null!;
    public List<StashedEvent> Events { get; set; } = [];

    /// <summary>
    /// Number of events currently checked out via Unstash and awaiting Ack/Nack.
    /// These events sit at the front of <see cref="Events"/> and must not be handed
    /// out again until the in-process batch is resolved.
    /// </summary>
    public int InProcessCount { get; set; }

    /// <summary>
    /// UTC timestamp when the current in-process batch was claimed. Used to expire
    /// stale claims so events don't stay in-process forever.
    /// </summary>
    public DateTime? InProcessClaimedAt { get; set; }

    public static string BuildId(ProjectionContextId id)
    {
        return $"projections/{id.ProjectionName.ToLower()}/stash/{id.ItemId.GetStringRepresentation().ToLower()}";
    }
}

[PublicAPI]
public class StashedEvent
{
    public StashedEvent()
    {
    }

    public StashedEvent(object @event, long? position)
    {
        Event = @event;
        Position = position;
    }

    public object Event { get; set; } = null!;
    public long? Position { get; set; }
}




