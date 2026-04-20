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
}
