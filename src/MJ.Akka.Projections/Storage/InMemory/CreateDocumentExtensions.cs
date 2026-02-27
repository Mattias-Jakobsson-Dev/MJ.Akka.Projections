using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class CreateDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        CreateDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, TDocument> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.CreateDocument((evnt, _) => create(evnt));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        CreateDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, DocumentHandlingMetaData<TIdContext>, TDocument> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.CreateDocument((evnt, metadata) => Task.FromResult(create(evnt, metadata)));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        CreateDocument<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
            Func<TEvent, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.CreateDocument(async (evnt, _, _) => await create(evnt));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        CreateDocument<TIdContext, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
        Func<TEvent, DocumentHandlingMetaData<TIdContext>, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.CreateDocument(async (evnt, metadata, _) => await create(evnt, metadata));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> 
        CreateDocument<TIdContext, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
        Func<TEvent, CancellationToken, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.CreateDocument(async (evnt, _, cancellationToken) => await create(evnt, cancellationToken));

    public static ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> 
        CreateDocument<TIdContext, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> setup,
        Func<TEvent, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TDocument>> create)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
            context.CreateDocument(await create(
                evnt, 
                new DocumentHandlingMetaData<TIdContext>(context.Id, position), 
                cancellationToken)));
    }
}