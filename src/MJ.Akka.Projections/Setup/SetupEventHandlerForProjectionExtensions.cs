using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionExtensions
{
    // -------------------------------------------------------------------------
    // WithId sync overload on ISetupEventRouting
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> WithId<TIdContext, TContext, TEvent>(
        this ISetupEventRouting<TIdContext, TContext, TEvent> routing,
        Func<TEvent, TIdContext?> getId)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => routing.WithId(evnt => Task.FromResult(getId(evnt)));

    // -------------------------------------------------------------------------
    // HandleWith convenience overloads on ISetupEventHandlerForProjection
    // -------------------------------------------------------------------------

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
        => setup.HandleWith((evnt, context, _, _) => handler(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.HandleWith((evnt, context, position, _) => handler(evnt, context, position));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, CancellationToken, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.HandleWith((evnt, context, _, cancellationToken) => handler(evnt, context, cancellationToken));

    // -------------------------------------------------------------------------
    // HandleWith convenience overloads on ISetupHandlerFiltering (pass-all filter)
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, CancellationToken, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.When(f => f, h => h.HandleWith(handler));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
        Action<TEvent, TContext> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.When(f => f, h => h.HandleWith(handler));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
        Action<TEvent, TContext, long?> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.When(f => f, h => h.HandleWith(handler));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.When(f => f, h => h.HandleWith(handler));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, long?, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.When(f => f, h => h.HandleWith(handler));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent> HandleWith<TIdContext, TContext, TEvent>(
        this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
        Func<TEvent, TContext, CancellationToken, Task> handler)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
        => setup.When(f => f, h => h.HandleWith(handler));
}