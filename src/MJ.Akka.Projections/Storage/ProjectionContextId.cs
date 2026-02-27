using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage;

public record ProjectionContextId(string ProjectionName, IProjectionIdContext ItemId)
{
    public virtual bool Equals(ProjectionContextId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return ProjectionName == other.ProjectionName && ItemId.Equals(other.ItemId);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProjectionName, ItemId.GetStringRepresentation());
    }
}