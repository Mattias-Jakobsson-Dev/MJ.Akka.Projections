using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class CreateBeforeModifyDocumentExtensions
{
    // -------------------------------------------------------------------------
    // Without TData
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, TDocument> create,
            Func<TEvent, TDocument, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            (evnt, _, _) => Task.FromResult(create(evnt)),
            (evnt, doc, _, _) => Task.FromResult(modify(evnt, doc)));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, TDocument> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            (evnt, metadata, _) => Task.FromResult(create(evnt, metadata)),
            (evnt, doc, metadata, _) => Task.FromResult(modify(evnt, doc, metadata)));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, Task<TDocument>> create,
            Func<TEvent, TDocument, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            (evnt, _, _) => create(evnt),
            (evnt, doc, _, _) => modify(evnt, doc));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            (evnt, metadata, _) => create(evnt, metadata),
            (evnt, doc, metadata, _) => modify(evnt, doc, metadata));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TDocument, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            (evnt, _, ct) => create(evnt, ct),
            (evnt, doc, _, ct) => modify(evnt, doc, ct));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
    {
        return setup
            .WhenDocumentNotExists<TIdContext, TDocument, TContext, TEvent>(
                builder => builder
                    .CreateDocument(create)
                    .ModifyDocument(modify))
            .WhenDocumentExists<TIdContext, TDocument, TContext, TEvent>(builder => builder
                .ModifyDocument(modify));
    }

    // -------------------------------------------------------------------------
    // With TData
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument> create,
            Func<TEvent, TData, TDocument, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            (evnt, data, _, _) => Task.FromResult(create(evnt, data)),
            (evnt, data, doc, _, _) => Task.FromResult(modify(evnt, data, doc)));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, TDocument> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            (evnt, data, metadata, _) => Task.FromResult(create(evnt, data, metadata)),
            (evnt, data, doc, metadata, _) => Task.FromResult(modify(evnt, data, doc, metadata)));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            (evnt, data, _, _) => create(evnt, data),
            (evnt, data, doc, _, _) => modify(evnt, data, doc));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            (evnt, data, metadata, _) => create(evnt, data, metadata),
            (evnt, data, doc, metadata, _) => modify(evnt, data, doc, metadata));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            (evnt, data, _, ct) => create(evnt, data, ct),
            (evnt, data, doc, _, ct) => modify(evnt, data, doc, ct));

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
    {
        return setup
            .WhenDocumentNotExists<TIdContext, TDocument, TContext, TEvent, TData>(
                builder => builder
                    .CreateDocument(create)
                    .ModifyDocument(modify))
            .WhenDocumentExists<TIdContext, TDocument, TContext, TEvent, TData>(builder => builder
                .ModifyDocument(modify));
    }
}