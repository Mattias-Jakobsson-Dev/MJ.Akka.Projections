using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class DeleteDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        DeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        => setup.HandleWith((_, context) => context.DeleteDocument());

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, _) => condition(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument((evnt, context, position) =>
        Task.FromResult(condition(evnt, context, position)));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class =>
        setup.ConditionallyDeleteDocument(async (evnt, context, _, _) => await condition(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, position, _) =>
        await condition(evnt, context, position));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ConditionallyDeleteDocument(async (evnt, context, _, cancellationToken) =>
        await condition(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument>, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, CancellationToken, Task<bool>> condition)
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