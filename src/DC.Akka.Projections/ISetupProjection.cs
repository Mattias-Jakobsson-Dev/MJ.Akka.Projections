using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public interface ISetupProjection<TId, TDocument> where TId : notnull where TDocument : notnull
{
    ISetupProjection<TId, TDocument> RegisterHandler<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, TDocument?> handler);
     
    ISetupProjection<TId, TDocument> RegisterHandler<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, long, TDocument?> handler);
        
    ISetupProjection<TId, TDocument> RegisterHandler<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, Task<TDocument?>> handler);
        
    ISetupProjection<TId, TDocument> RegisterHandler<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, long, Task<TDocument?>> handler);
        
    IHandleEventInProjection<TId, TDocument> Build();
}