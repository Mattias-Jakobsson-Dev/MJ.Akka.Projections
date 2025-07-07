using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

[PublicAPI]
public static class CreateDocumentExtensions
{
    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, TDocument> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument((evnt, _) => create(evnt));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, long?, TDocument> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument((evnt, position) => Task.FromResult(create(evnt, position)));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<TEvent, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument(async (evnt, _, _) => await create(evnt));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, long?, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument(async (evnt, position, _) => await create(evnt, position));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> 
        CreateDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, CancellationToken, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument(async (evnt, _, cancellationToken) => await create(evnt, cancellationToken));

    public static ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> 
        CreateDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
        Func<TEvent, long?, CancellationToken, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
            context.CreateDocument(await create(evnt, position, cancellationToken)));
    }
}