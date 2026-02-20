using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Setup;

public interface ISetupEventHandlerForProjection<TId, TContext, out TEvent> 
    : ISetupProjectionHandlers<TId, TContext>
    where TId : notnull
    where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler);
}

[PublicAPI]
public interface ISetupProjection<TId, TContext> : ISetupProjectionHandlers<TId, TContext> 
    where TId : notnull where TContext : IProjectionContext
{
    ISetupProjection<TId, TContext> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform);
}

public interface ISetupProjectionHandlers<TId, TContext> where TId : notnull where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TId, TContext, TEvent> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);
    
    IHandleEventInProjection<TId, TContext> Build();
}