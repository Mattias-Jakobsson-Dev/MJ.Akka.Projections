using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class ModifyDocumentExtensions
{
    // -------------------------------------------------------------------------
    // Non-nullable ModifyDocument (WhenExists path)
    // Inference works because TDocument is explicit in ISetupEventHandlerForContextWithExistingDocument.
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, _, _) =>
        {
            ctx.ModifyDocument(doc => modify(evnt, doc!));
            return Task.CompletedTask;
        });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, _, _) => ctx.ModifyDocument(doc => modify(evnt, doc!)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, _, ct) => ctx.ModifyDocument(doc => modify(evnt, doc!, ct)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, position, _) =>
        {
            ctx.ModifyDocument(doc => modify(evnt, doc!, new DocumentHandlingMetaData<TIdContext>(ctx.Id, position)));
            return Task.CompletedTask;
        });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, position, _) =>
            ctx.ModifyDocument(doc => modify(evnt, doc!, new DocumentHandlingMetaData<TIdContext>(ctx.Id, position))));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, position, ct) =>
            ctx.ModifyDocument(doc => modify(evnt, doc!, new DocumentHandlingMetaData<TIdContext>(ctx.Id, position), ct)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    // -------------------------------------------------------------------------
    // TData ModifyDocument overloads (WithData path)
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, data, _, _) =>
        {
            ctx.ModifyDocument(doc => modify(evnt, data, doc!));
            return Task.CompletedTask;
        });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, data, _, _) => ctx.ModifyDocument(doc => modify(evnt, data, doc!)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, data, _, ct) => ctx.ModifyDocument(doc => modify(evnt, data, doc!, ct)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, data, position, _) =>
        {
            ctx.ModifyDocument(doc => modify(evnt, data, doc!, new DocumentHandlingMetaData<TIdContext>(ctx.Id, position)));
            return Task.CompletedTask;
        });
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, data, position, _) =>
            ctx.ModifyDocument(doc => modify(evnt, data, doc!, new DocumentHandlingMetaData<TIdContext>(ctx.Id, position))));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        ModifyDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith((evnt, ctx, data, position, ct) =>
            ctx.ModifyDocument(doc => modify(evnt, data, doc!, new DocumentHandlingMetaData<TIdContext>(ctx.Id, position), ct)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }
}
