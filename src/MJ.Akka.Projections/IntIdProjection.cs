using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public abstract class IntIdProjection<TDocument> : BaseProjection<int, TDocument> where TDocument : notnull
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