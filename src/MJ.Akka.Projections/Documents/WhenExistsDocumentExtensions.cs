using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class WhenExistsDocumentExtensions
{
    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        WhenDocumentExists<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>,
                IProjectionFilterSetup<TIdContext, TContext, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(h)));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        WhenDocumentNotExists<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>,
                IProjectionFilterSetup<TIdContext, TContext, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(h)));

    // -------------------------------------------------------------------------
    // TData overloads
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        WhenDocumentExists<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>,
                IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(h)));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        WhenDocumentNotExists<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>,
                IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(h)));
}
