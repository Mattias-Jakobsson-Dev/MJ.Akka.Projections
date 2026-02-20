using JetBrains.Annotations;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionExtensions
{
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Action<TEvent, TContext> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, _) =>
        {
            handler(evnt, context);

            return Task.CompletedTask;
        });
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Action<TEvent, TContext, long?> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, position, _) =>
        {
            handler(evnt, context, position);

            return Task.CompletedTask;
        });
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, Task> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, _) => handler(evnt, context));
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, Task> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, position, _) => handler(evnt, context, position));
    }
    
    public static ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith<TId, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TId, TContext, TEvent> setup,
        Func<TEvent, TContext, CancellationToken, Task> handler)
        where TId : notnull
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, cancellationToken) => handler(evnt, context, cancellationToken));
    }
}