using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public abstract class IntIdProjection<TDocument> : IProjection<int, TDocument> where TDocument : notnull
{
    public virtual int IdFromString(string id)
    {
        return int.Parse(id);
    }

    public virtual string IdToString(int id)
    {
        return id.ToString();
    }
    
    public abstract ISetupProjection<int, TDocument> Configure(ISetupProjection<int, TDocument> config);
    public abstract Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
}