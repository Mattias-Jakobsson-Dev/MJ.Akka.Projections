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
            Func<TEvent, TDocument, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, _, _) => Task.FromResult(create(evnt)),
            (evnt, doc, _, _) => Task.FromResult(modify(evnt, doc)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, TDocument> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, metadata, _) => Task.FromResult(create(evnt, metadata)),
            (evnt, doc, metadata, _) => Task.FromResult(modify(evnt, doc, metadata)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, TDocument> create,
            Func<TEvent, TDocument, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, _, _) => Task.FromResult(create(evnt)),
            (evnt, doc, _, _) => modify(evnt, doc),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, Task<TDocument>> create,
            Func<TEvent, TDocument, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, _, _) => create(evnt),
            (evnt, doc, _, _) => Task.FromResult(modify(evnt, doc)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, TDocument> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, metadata, _) => Task.FromResult(create(evnt, metadata)),
            (evnt, doc, metadata, _) => modify(evnt, doc, metadata),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, metadata, _) => create(evnt, metadata),
            (evnt, doc, metadata, _) => Task.FromResult(modify(evnt, doc, metadata)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, Task<TDocument>> create,
            Func<TEvent, TDocument, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, _, _) => create(evnt),
            (evnt, doc, _, _) => modify(evnt, doc),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, metadata, _) => create(evnt, metadata),
            (evnt, doc, metadata, _) => modify(evnt, doc, metadata),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TDocument, CancellationToken, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, _, ct) => create(evnt, ct),
            (evnt, doc, _, ct) => modify(evnt, doc, ct),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TDocument, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
    {
        extraSetup ??= x => x;
        
        return setup
            .WhenDocumentNotExists<TIdContext, TDocument, TContext, TEvent>(
                builder => extraSetup(builder
                    .CreateDocument(create)
                    .ModifyDocument(modify)))
            .WhenDocumentExists<TIdContext, TDocument, TContext, TEvent>(builder => extraSetup(builder
                .ModifyDocument(modify)));
    }

    // -------------------------------------------------------------------------
    // With TData
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument> create,
            Func<TEvent, TData, TDocument, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, _, _) => Task.FromResult(create(evnt, data)),
            (evnt, data, doc, _, _) => Task.FromResult(modify(evnt, data, doc)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, TDocument> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, metadata, _) => Task.FromResult(create(evnt, data, metadata)),
            (evnt, data, doc, metadata, _) => Task.FromResult(modify(evnt, data, doc, metadata)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, TDocument> create,
            Func<TEvent, TData, TDocument, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, _, _) => Task.FromResult(create(evnt, data)),
            (evnt, data, doc, _, _) => modify(evnt, data, doc),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, _, _) => create(evnt, data),
            (evnt, data, doc, _, _) => Task.FromResult(modify(evnt, data, doc)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, TDocument> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, metadata, _) => Task.FromResult(create(evnt, data, metadata)),
            (evnt, data, doc, metadata, _) => modify(evnt, data, doc, metadata),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, TDocument> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, metadata, _) => create(evnt, data, metadata),
            (evnt, data, doc, metadata, _) => Task.FromResult(modify(evnt, data, doc, metadata)),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, _, _) => create(evnt, data),
            (evnt, data, doc, _, _) => modify(evnt, data, doc),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, metadata, _) => create(evnt, data, metadata),
            (evnt, data, doc, metadata, _) => modify(evnt, data, doc, metadata),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, CancellationToken, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
        => setup.EnsureCreatedThenModify(
            (evnt, data, _, ct) => create(evnt, data, ct),
            (evnt, data, doc, _, ct) => modify(evnt, data, doc, ct),
            extraSetup);

    public static ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
        EnsureCreatedThenModify<TIdContext, TDocument, TContext, TEvent, TData>(
            this ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create,
            Func<TEvent, TData, TDocument, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify,
            Func<
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>,
                ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent, TData>>? extraSetup = null)
        where TIdContext : IProjectionIdContext
        where TContext : ContextWithDocument<TIdContext, TDocument>
        where TDocument : class
    {
        extraSetup ??= x => x;

        return setup
            .WhenDocumentNotExists<TIdContext, TDocument, TContext, TEvent, TData>(
                builder => extraSetup(builder
                    .CreateDocument(create)
                    .ModifyDocument(modify)))
            .WhenDocumentExists<TIdContext, TDocument, TContext, TEvent, TData>(builder => extraSetup(builder
                .ModifyDocument(modify)));
    }
}