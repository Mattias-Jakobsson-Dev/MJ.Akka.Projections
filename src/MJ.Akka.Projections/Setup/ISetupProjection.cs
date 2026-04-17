using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public interface ISetupProjection<TIdContext, TContext> 
    where TIdContext : IProjectionIdContext 
    where TContext : IProjectionContext
{
    ISetupEventRouting<TIdContext, TContext, TEvent> On<TEvent>();
    
    IHandleEventInProjection<TIdContext, TContext> Build();
}

public interface ISetupEventRouting<TIdContext, TContext, TEvent> where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupProjection<TIdContext, TContext> Transform(Func<TEvent, IImmutableList<object>> transform);
    
    ISetupHandlerFiltering<TIdContext, TContext, TEvent> WithId(Func<TEvent, Task<TIdContext?>> getId);
}

public interface ISetupHandlerFiltering<TIdContext, TContext, TEvent> : ISetupProjection<TIdContext, TContext>  
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupHandlerFiltering<TIdContext, TContext, TEvent> When(
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>> filter,
        Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers);
}

public interface ISetupEventHandlerForProjection<TIdContext, out TContext, out TEvent>
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler);
}