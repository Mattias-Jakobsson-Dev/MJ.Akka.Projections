using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public abstract class LongIdProjection<TContext> : BaseProjection<long, TContext> 
    where TContext : IProjectionContext<long>
{
    public override long IdFromString(string id)
    {
        return int.Parse(id);
    }

    public override string IdToString(long id)
    {
        return id.ToString();
    }
}