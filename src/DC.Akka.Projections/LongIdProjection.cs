using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public abstract class LongIdProjection<TDocument> : BaseProjection<long, TDocument> where TDocument : notnull
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