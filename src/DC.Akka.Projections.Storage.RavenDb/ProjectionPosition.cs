namespace DC.Akka.Projections.Storage.RavenDb;

public class ProjectionPosition
{
    public ProjectionPosition()
    {
        
    }

    public ProjectionPosition(string projectionName)
    {
        Id = BuildId(projectionName);
        ProjectionName = projectionName;
    }
    
    public string Id { get; set; } = null!;
    public long Position { get; set; }
    public string ProjectionName { get; set; } = null!;

    public static string BuildId(string projectionName)
    {
        return $"projections/{projectionName.ToLower()}/positions";
    }
}