using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public abstract class StringIdProjection<TDocument> : IProjection<string, TDocument> where TDocument : notnull
{
    public virtual string IdFromString(string id)
    {
        return id;
    }

    public virtual string IdToString(string id)
    {
        return id;
    }
    
    public abstract ISetupProjection<string, TDocument> Configure(ISetupProjection<string, TDocument> config);
    public abstract Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
}

public interface IProjection<TId, TDocument> where TId : notnull where TDocument : notnull
{
    string Name => GetType().Name;
    TId IdFromString(string id);
    string IdToString(TId id);
    ISetupProjection<TId, TDocument> Configure(ISetupProjection<TId, TDocument> config);
    Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
}