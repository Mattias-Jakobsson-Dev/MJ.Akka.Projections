using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class ModifyDocumentExtensions
{
    // -------------------------------------------------------------------------
    // WhenExists / WhenNotExists on ISetupHandlerFiltering
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        WhenExists<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent>,
                ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>,
                IProjectionFilterSetup<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForProjectionWithDocument<TIdContext, TDocument, TEvent>(h)));

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        WhenNotExists<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForProjectionWithoutDocument<TIdContext, TDocument, TEvent>,
                ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>,
                IProjectionFilterSetup<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>>? additionalFilter = null)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForProjectionWithDocument<TIdContext, TDocument, TEvent>(h)));

    // -------------------------------------------------------------------------
    // ModifyDocument on ISetupHandlerFiltering (convenience — pass-all filter)
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<string>>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<string>>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupHandlerFiltering<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<string>>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    // -------------------------------------------------------------------------
    // ModifyDocument on ISetupEventHandlerForProjection (nullable document)
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument((evnt, doc, _) => modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<string>>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument((evnt, doc, metadata) => Task.FromResult(modify(evnt, doc, metadata)));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, _) => await modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<string>>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, metadata, _) => await modify(evnt, doc, metadata));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, ct) => await modify(evnt, doc, ct));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<string>>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith((evnt, context, position, cancellationToken) =>
            context.ModifyDocument(doc => modify(
                evnt, doc,
                new DocumentHandlingMetaData<SimpleIdContext<string>>(context.Id, position),
                cancellationToken)));
    }

    // -------------------------------------------------------------------------
    // Non-nullable ModifyDocument (WhenExists path)
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, TDocument, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.HandleWith((evnt, ctx, _, _) =>
        {
            ctx.ModifyDocument(doc => modify(evnt, doc!));
            return Task.CompletedTask;
        });

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, TDocument, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.HandleWith((evnt, ctx, _, _) => ctx.ModifyDocument(doc => modify(evnt, doc!)));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, TDocument, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.HandleWith((evnt, ctx, _, ct) => ctx.ModifyDocument(doc => modify(evnt, doc!, ct)));
}