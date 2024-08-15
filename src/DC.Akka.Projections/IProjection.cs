using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public interface IProjection<TId, TDocument> where TId : notnull where TDocument : notnull
{
    string Name => GetType().Name;
    TId IdFromString(string id);
    string IdToString(TId id);
    ISetupProjection<TId, TDocument> Configure(ISetupProjection<TId, TDocument> config);
    Source<EventWithPosition, NotUsed> StartSource(long? fromPosition);
}