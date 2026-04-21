using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.InMemory;

// ReSharper disable once CheckNamespace
namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class InMemoryStorageWhenDocumentExtensions
{
    // -------------------------------------------------------------------------
    // WhenDocumentExists / WhenDocumentNotExists
    // TDocument is a direct type argument of InMemoryProjectionContext in `this`,
    // so the compiler infers it without needing an explicit type argument.
    // -------------------------------------------------------------------------
    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        WhenDocumentExists<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent>,
                ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>,
                IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>>? additionalFilter = null)
        where TId : notnull
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent>(h)));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
        WhenDocumentNotExists<TId, TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent>,
                ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>,
                IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>>? additionalFilter = null)
        where TId : notnull
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent>(h)));

    // -------------------------------------------------------------------------
    // TData overloads – TDocument is still a direct type arg of InMemoryProjectionContext
    // in `this`, so inference works the same way as the non-data overloads above.
    // -------------------------------------------------------------------------
    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>
        WhenDocumentExists<TId, TDocument, TEvent, TData>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData> setup,
            Func<ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>,
                ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>,
                IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>>? additionalFilter = null)
        where TId : notnull
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>(h)));

    public static ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>
        WhenDocumentNotExists<TId, TDocument, TEvent, TData>(
            this ISetupHandlerFiltering<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData> setup,
            Func<ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>,
                ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>,
                IProjectionFilterSetup<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>>? additionalFilter = null)
        where TId : notnull
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new SetupEventHandlerForContextWithDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent, TData>(h)));
}
