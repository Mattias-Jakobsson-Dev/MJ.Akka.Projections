using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

public interface ISetupEventHandlerForProjection<TIdContext, TContext, out TEvent> 
    : ISetupProjectionHandlers<TIdContext, TContext>
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler);
}

[PublicAPI]
public interface ISetupProjection<TIdContext, TContext> : ISetupProjectionHandlers<TIdContext, TContext> 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    ISetupProjection<TIdContext, TContext> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform);
}

public interface ISetupProjectionHandlers<TIdContext, TContext> 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> On<TEvent>(
        Func<TEvent, TIdContext?> getId,
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>>? filter =
            null)
        => On(evnt => Task.FromResult(getId(evnt)),
            filter);
    
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> On<TEvent>(
        Func<TEvent, Task<TIdContext?>> getId,
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>>? filter = null);
    
    IHandleEventInProjection<TIdContext, TContext> Build();
}