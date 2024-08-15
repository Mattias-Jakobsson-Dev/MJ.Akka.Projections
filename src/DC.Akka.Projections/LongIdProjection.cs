using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public abstract class LongIdProjection<TDocument> : IProjection<long, TDocument> where TDocument : notnull
{
    public virtual long IdFromString(string id)
    {
        return int.Parse(id);
    }

    public virtual string IdToString(long id)
    {
        return id.ToString();
    }
    
    public abstract ISetupProjection<long, TDocument> Configure(ISetupProjection<long, TDocument> config);
    public abstract Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
}