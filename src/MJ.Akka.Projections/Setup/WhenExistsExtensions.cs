using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

[PublicAPI]
public static class SetupEventHandlerForProjectionWithDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        WhenExists<TIdContext, TContext, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.When(f => f.WithDocumentFilter(ctx => ctx.Exists()));
    }

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        WhenNotExists<TIdContext, TContext, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> setup)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.When(f => f.WithDocumentFilter(ctx => !ctx.Exists()));
    }
}
