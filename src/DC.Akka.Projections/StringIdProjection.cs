using Akka;
using Akka.Streams.Dsl;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
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