using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class DeleteDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        DeleteDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith((_, context) => context.DeleteDocument());
    }
    
    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TId, TDocument>, bool> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, _) => condition(evnt, context));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TId, TDocument>, long?, bool> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, position) =>
        Task.FromResult(condition(evnt, context, position)));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TId, TDocument>, Task<bool>> condition)
        where TId : notnull
        where TDocument : class =>
        setup.ConditionallyDeleteDocument(async (evnt, context, _, _) => await condition(evnt, context));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TId, TDocument>, long?, Task<bool>> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, position, _) =>
        await condition(evnt, context, position));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TId, TDocument>, CancellationToken, Task<bool>> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, _, cancellationToken) =>
        await condition(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TId, TDocument>, long?, CancellationToken, Task<bool>> condition)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            if (await condition(evnt, context, position, cancellationToken))
                context.DeleteDocument();
        });
    }
}