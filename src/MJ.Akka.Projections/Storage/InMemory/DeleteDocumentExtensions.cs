using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class DeleteDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        DeleteDocument<TIdContext, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith((_, context) => context.DeleteDocument());
    }
    
    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, _) => condition(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, long?, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, position) =>
        Task.FromResult(condition(evnt, context, position)));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class =>
        setup.ConditionallyDeleteDocument(async (evnt, context, _, _) => await condition(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, long?, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, position, _) =>
        await condition(evnt, context, position));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, _, cancellationToken) =>
        await condition(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, long?, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            if (await condition(evnt, context, position, cancellationToken))
                context.DeleteDocument();
        });
    }
}