using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionWithDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        WhenExists<TIdContext, TContext, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.When(f =>
        {
            var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
            return additionalFilter != null ? additionalFilter(filtered) : filtered;
        });
    }

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        WhenNotExists<TIdContext, TContext, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.When(f =>
        {
            var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
            return additionalFilter != null ? additionalFilter(filtered) : filtered;
        });
    }
}
