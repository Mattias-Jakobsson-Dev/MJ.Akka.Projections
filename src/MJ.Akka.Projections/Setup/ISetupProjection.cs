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
    
    ISetupHandlerFiltering<TIdContext, TContext, TEvent> WithId(Func<TEvent, TIdContext?> getId);
    
    ISetupEventRouting<TIdContext, TContext, TEvent, TData> WithData<TData>(Func<TEvent, Task<TData>> getData);
}

public interface ISetupEventRouting<TIdContext, TContext, TEvent, TData> 
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupProjection<TIdContext, TContext> Transform(Func<TEvent, TData, IImmutableList<object>> transform);
    
    ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> WithId(Func<TEvent, TData, TIdContext?> getId);
}

public interface ISetupHandlerFiltering<TIdContext, TContext, TEvent> : ISetupProjection<TIdContext, TContext>  
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupHandlerFiltering<TIdContext, TContext, TEvent> When(
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>> filter,
        Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers);
}

public interface ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> : ISetupProjection<TIdContext, TContext>  
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> When(
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>, IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>> filter,
        Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>> configureHandlers);
}

public interface ISetupEventHandlerForProjection<TIdContext, out TContext, out TEvent>
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler);
}

public interface ISetupEventHandlerForProjection<TIdContext, out TContext, out TEvent, out TData>
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData> HandleWith(
        Func<TEvent, TContext, TData, long?, CancellationToken, Task> handler);
}

public static class SetupHandlerFilteringExtensions
{
    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> WhenAny<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> filtering,
        Func<
            ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>, 
            ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return filtering.When(x => x, configureHandlers);
    }
    
    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> WhenAny<TIdContext, TContext, TEvent, TData>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> filtering,
        Func<
            ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>, 
            ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>> configureHandlers)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return filtering.When(x => x, configureHandlers);
    }
}