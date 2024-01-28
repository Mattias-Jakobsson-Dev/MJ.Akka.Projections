using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.Projections;

public interface IProjection<TDocument>
{
    string Name { get; }
    ISetupProjection<TDocument> Configure(ISetupProjection<TDocument> config);
    Source<IProjectionSourceData, NotUsed> StartSource(long? fromPosition);
}