using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class DeleteDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        DeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith((_, context) => context.DeleteDocument());
    }

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, bool> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, _) => condition(evnt, context));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, bool> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, position) =>
        Task.FromResult(condition(evnt, context, position)));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, Task<bool>> condition)
        where TId : notnull
        where TDocument : class =>
        setup.ConditionallyDeleteDocument(async (evnt, context, _, _) => await condition(evnt, context));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, Task<bool>> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, position, _) =>
        await condition(evnt, context, position));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, CancellationToken, Task<bool>> condition)
        where TId : notnull
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, _, cancellationToken) =>
        await condition(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, CancellationToken, Task<bool>> condition)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
            await condition(evnt, context, position, cancellationToken) ? context.DeleteDocument() : []);
    }
}