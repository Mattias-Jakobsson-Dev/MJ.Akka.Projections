using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionExtensions
{
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, IEnumerable<IProjectionResult>> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, _) => Task.FromResult(handler(evnt, context)));
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, IEnumerable<IProjectionResult>> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, position, _) => 
            Task.FromResult(handler(evnt, context, position)));
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, Task<IEnumerable<IProjectionResult>>> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, _) => handler(evnt, context));
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, Task<IEnumerable<IProjectionResult>>> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, position, _) => handler(evnt, context, position));
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, CancellationToken, Task<IEnumerable<IProjectionResult>>> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, cancellationToken) => handler(evnt, context, cancellationToken));
    }
}