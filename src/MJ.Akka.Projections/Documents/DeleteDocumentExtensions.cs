using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class DeleteDocumentExtensions
{
    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        DeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((_, context, _, _) => { context.DeleteDocument(); return Task.CompletedTask; });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, context, _) => condition(evnt, context));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, long?, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, context, position) =>
            Task.FromResult(condition(evnt, context, position)));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, context, _, _) => await condition(evnt, context));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, long?, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, context, position, _) =>
            await condition(evnt, context, position));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, context, _, cancellationToken) =>
            await condition(evnt, context, cancellationToken));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TContext, long?, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            if (await condition(evnt, context, position, cancellationToken))
                context.DeleteDocument();
        });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        DeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((_, context, _, _, _) => { context.DeleteDocument(); return Task.CompletedTask; });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TContext, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, _, context, _, _) =>
            Task.FromResult(condition(evnt, context)));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TContext, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, _, context, _, _) =>
            await condition(evnt, context));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TContext, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, data, context, _, _) =>
            Task.FromResult(condition(evnt, data, context)));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TContext, long?, bool> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument((evnt, data, context, position, _) =>
            Task.FromResult(condition(evnt, data, context, position)));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TContext, long?, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, data, context, position, _) =>
            await condition(evnt, data, context, position));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TContext, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, data, context, _, _) =>
            await condition(evnt, data, context));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TContext, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.ConditionallyDeleteDocument(async (evnt, data, context, _, ct) =>
            await condition(evnt, data, context, ct));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ConditionallyDeleteDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TContext, long?, CancellationToken, Task<bool>> condition)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith(async (evnt, context, data, position, cancellationToken) =>
        {
            if (await condition(evnt, data, context, position, cancellationToken))
                context.DeleteDocument();
        });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }
}
