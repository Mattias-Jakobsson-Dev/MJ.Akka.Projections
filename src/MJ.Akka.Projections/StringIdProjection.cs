using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public abstract class StringIdProjection<TContext> : BaseProjection<string, TContext> 
    where TContext : IProjectionContext<string>
{
    public override string IdFromString(string id)
    {
        return id;
    }

    public override string IdToString(string id)
    {
        return id;
    }
}