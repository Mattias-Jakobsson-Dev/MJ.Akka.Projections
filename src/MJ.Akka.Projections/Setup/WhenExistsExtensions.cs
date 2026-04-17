using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionWithDocumentExtensions
{
    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        WhenExists<TIdContext, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            configureHandlers);
    }

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        WhenNotExists<TIdContext, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            configureHandlers);
    }
}
