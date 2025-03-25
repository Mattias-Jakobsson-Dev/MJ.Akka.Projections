using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public abstract class StringIdProjection<TDocument> : BaseProjection<string, TDocument> where TDocument : notnull
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