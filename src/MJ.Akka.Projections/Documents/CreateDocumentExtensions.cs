using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class CreateDocumentExtensions
{
    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        CreateDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, TDocument> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument((evnt, _) => create(evnt));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        CreateDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, TDocument> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument((evnt, metadata) => Task.FromResult(create(evnt, metadata)));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        CreateDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument(async (evnt, _, _) => await create(evnt));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        CreateDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument(async (evnt, metadata, _) => await create(evnt, metadata));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        CreateDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, CancellationToken, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument(async (evnt, _, cancellationToken) => await create(evnt, cancellationToken));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>
        CreateDocument<TIdContext, TDocument, TContext, TEvent>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith(async (evnt, context, position, cancellationToken) =>
            context.CreateDocument(await create(
                evnt,
                new DocumentHandlingMetaData<TIdContext>(context.Id, position),
                cancellationToken)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(handler);
    }

    // -------------------------------------------------------------------------
    // TData CreateDocument overloads (WithData path)
    // -------------------------------------------------------------------------

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        CreateDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument((evnt, data, _) => create(evnt, data));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        CreateDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, TDocument> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument((evnt, data, metadata) => Task.FromResult(create(evnt, data, metadata)));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        CreateDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument(async (evnt, data, _, _) => await create(evnt, data));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        CreateDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument(async (evnt, data, metadata, _) => await create(evnt, data, metadata));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        CreateDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, CancellationToken, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
        => setup.CreateDocument(async (evnt, data, _, cancellationToken) => await create(evnt, data, cancellationToken));

    public static ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>
        CreateDocument<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
        where TContext : ContextWithDocument<TIdContext, TDocument>
    {
        var handler = setup.HandleWith(async (evnt, context, data, position, cancellationToken) =>
            context.CreateDocument(await create(
                evnt,
                data,
                new DocumentHandlingMetaData<TIdContext>(context.Id, position),
                cancellationToken)));
        return new SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent, TData>(handler);
    }
}
