using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Action<TEvent, TContext> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, _) =>
        {
            handler(evnt, context);

            return Task.CompletedTask;
        });
    }
    
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Action<TEvent, TContext, long?> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, position, _) =>
        {
            handler(evnt, context, position);

            return Task.CompletedTask;
        });
    }
    
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, _) => handler(evnt, context));
    }
    
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, position, _) => handler(evnt, context, position));
    }
    
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, CancellationToken, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.HandleWith((evnt, context, _, cancellationToken) => handler(evnt, context, cancellationToken));
    }
}