using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class ModifyDocumentExtensions
{
    // -------------------------------------------------------------------------
    // WhenExists / WhenNotExists on ISetupHandlerFiltering
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        WhenExists<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent>,
                ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>,
                IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>>? additionalFilter = null)
        where TId : notnull
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForProjectionWithDocument<TId, TDocument, TEvent>(h)));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        WhenNotExists<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForProjectionWithoutDocument<TId, TDocument, TEvent>,
                ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>,
                IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>>? additionalFilter = null)
        where TId : notnull
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForProjectionWithDocument<TId, TDocument, TEvent>(h)));

    // -------------------------------------------------------------------------
    // ModifyDocument on ISetupHandlerFiltering (convenience — pass-all filter)
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TId : notnull
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<TId>>, TDocument> modify)
        where TId : notnull
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<TId>>, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<TId>>, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
        => setup.When(f => f, h => h.ModifyDocument(modify));

    // -------------------------------------------------------------------------
    // ModifyDocument on ISetupEventHandlerForProjection (nullable document)
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument((evnt, doc, _) => modify(evnt, doc));

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<TId>>, TDocument> modify)
        where TId : notnull
        where TDocument : class =>
        setup.ModifyDocument((evnt, doc, metadata) => Task.FromResult(modify(evnt, doc, metadata)));

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, _) => await modify(evnt, doc));

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<TId>>, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class =>
        setup.ModifyDocument(async (evnt, doc, metadata, _) => await modify(evnt, doc, metadata));

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, ct) => await modify(evnt, doc, ct));

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<SimpleIdContext<TId>>, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith((evnt, context, position, cancellationToken) =>
            context.ModifyDocument(doc => modify(
                evnt, doc,
                new DocumentHandlingMetaData<SimpleIdContext<TId>>(context.Id, position),
                cancellationToken)));
    }

    // -------------------------------------------------------------------------
    // Non-nullable ModifyDocument (WhenExists path)
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent> setup,
            Func<TEvent, TDocument, TDocument> modify)
        where TId : notnull
        where TDocument : class
        => setup.HandleWith((evnt, ctx, _, _) =>
        {
            ctx.ModifyDocument(doc => modify(evnt, doc!));
            return Task.CompletedTask;
        });

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent> setup,
            Func<TEvent, TDocument, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
        => setup.HandleWith((evnt, ctx, _, _) => ctx.ModifyDocument(doc => modify(evnt, doc!)));

    public static ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent> setup,
            Func<TEvent, TDocument, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
        => setup.HandleWith((evnt, ctx, _, ct) => ctx.ModifyDocument(doc => modify(evnt, doc!, ct)));
}