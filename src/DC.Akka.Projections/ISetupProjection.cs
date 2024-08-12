using System.Collections.Immutable;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public interface ISetupProjection<TId, TDocument> where TId : notnull where TDocument : notnull
{
    ISetupProjection<TId, TDocument> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform);
    
    ISetupProjection<TId, TDocument> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, TDocument?> handler,
        Func<IProjectionFilterSetup<TDocument, TEvent>, IProjectionFilterSetup<TDocument, TEvent>>? filter = null);
     
    ISetupProjection<TId, TDocument> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, long, TDocument?> handler,
        Func<IProjectionFilterSetup<TDocument, TEvent>, IProjectionFilterSetup<TDocument, TEvent>>? filter = null);
        
    ISetupProjection<TId, TDocument> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, Task<TDocument?>> handler,
        Func<IProjectionFilterSetup<TDocument, TEvent>, IProjectionFilterSetup<TDocument, TEvent>>? filter = null);
        
    ISetupProjection<TId, TDocument> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TDocument?, long, Task<TDocument?>> handler,
        Func<IProjectionFilterSetup<TDocument, TEvent>, IProjectionFilterSetup<TDocument, TEvent>>? filter = null);
        
    IHandleEventInProjection<TId, TDocument> Build();
}