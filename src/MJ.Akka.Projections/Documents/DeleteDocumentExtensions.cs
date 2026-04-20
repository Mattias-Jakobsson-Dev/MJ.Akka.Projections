using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class DeleteDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        DeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.HandleWith((_, context, _, _) => { context.DeleteDocument(); return Task.CompletedTask; });

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, context, _) => condition(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, long?, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, context, position) =>
            Task.FromResult(condition(evnt, context, position)));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, context, _, _) => await condition(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, long?, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, context, position, _) =>
            await condition(evnt, context, position));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, context, _, cancellationToken) =>
            await condition(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, long?, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            if (await condition(evnt, context, position, cancellationToken))
                context.DeleteDocument();
        });
    }
}

