using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class ModifyDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument((evnt, doc, _) => modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, long?, TDocument> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument((evnt, doc, position) => Task.FromResult(modify(evnt, doc, position)));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> 
        ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, _) => await modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> 
        ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, long?, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, position, _) => await modify(evnt, doc, position));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, cancellationToken) => await modify(evnt, doc, cancellationToken));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, long?, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith((evnt, context, position, cancellationToken) =>
            context.ModifyDocument(doc => modify(evnt, doc, position, cancellationToken)));
    }
}