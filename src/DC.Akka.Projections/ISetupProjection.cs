namespace DC.Akka.Projections;

public interface ISetupProjection<TDocument>
{
    ISetupProjection<TDocument> RegisterHandler<TEvent>(
        Func<TEvent, object> getId,
        Func<TEvent, TDocument?, TDocument?> handler);
     
    ISetupProjection<TDocument> RegisterHandler<TEvent>(
        Func<TEvent, object> getId,
        Func<TEvent, TDocument?, long, TDocument?> handler);
        
    ISetupProjection<TDocument> RegisterHandler<TEvent>(
        Func<TEvent, object> getId,
        Func<TEvent, TDocument?, Task<TDocument?>> handler);
        
    ISetupProjection<TDocument> RegisterHandler<TEvent>(
        Func<TEvent, object> getId,
        Func<TEvent, TDocument?, long, Task<TDocument?>> handler);
        
    IHandleEventInProjection<TDocument> Build();
}