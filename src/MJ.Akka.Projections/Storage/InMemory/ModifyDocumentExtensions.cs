using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class ModifyDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
                setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument((evnt, doc, _) => modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
                setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<TIdContext>, TDocument> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class =>
        setup.ModifyDocument((evnt, doc, metadata) => Task.FromResult(modify(evnt, doc, metadata)));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
                setup,
            Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, _) => await modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
                setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class =>
        setup.ModifyDocument(async (evnt, doc, metadata, _) => await modify(evnt, doc, metadata));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
                setup,
            Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, cancellationToken) =>
        await modify(evnt, doc, cancellationToken));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ModifyDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
                setup,
            Func<TEvent, TDocument?, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> modify)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith((evnt, context, position, cancellationToken) =>
            context.ModifyDocument(doc => modify(
                evnt,
                doc,
                new DocumentHandlingMetaData<TIdContext>(context.Id, position),
                cancellationToken)));
    }
}