using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public abstract class IntIdProjection<TContext> : BaseProjection<int, TContext> where TContext : IProjectionContext<int>
{
    public override int IdFromString(string id)
    {
        return int.Parse(id);
    }

    public override string IdToString(int id)
    {
        return id.ToString();
    }
}