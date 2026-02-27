namespace MJ.Akka.Projections.ProjectionIds;

public record SimpleIdContext<TId>(TId Id) : IProjectionIdContext where TId : notnull
{
    public bool Equals(IProjectionIdContext? other)
    {
        return other is SimpleIdContext<TId> context &&
               EqualityComparer<TId>.Default.Equals(Id, context.Id);
    }

    public string GetStringRepresentation()
    {
        return Id.ToString() ?? "";
    }

    public override string ToString()
    {
        return GetStringRepresentation();
    }

    public static implicit operator SimpleIdContext<TId>(TId id) => new(id);
    public static implicit operator TId(SimpleIdContext<TId> context) => context.Id;
}