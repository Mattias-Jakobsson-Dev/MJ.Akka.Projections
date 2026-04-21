using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.RavenDb;

// ReSharper disable once CheckNamespace
namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class RavenDbStorageWhenDocumentExtensions
{
    // -------------------------------------------------------------------------
    // WhenDocumentExists / WhenDocumentNotExists
    // TDocument is a direct type argument of RavenDbProjectionContext in `this`,
    // so the compiler infers it without needing an explicit type argument.
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>
        WhenDocumentExists<TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent>,
                ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>,
                IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>>? additionalFilter = null)
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new RavenDbDocumentHandlerWrapper<TDocument, TEvent>(h)));

    public static ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>
        WhenDocumentNotExists<TDocument, TEvent>(
            this ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent>,
                ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>,
                IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>>? additionalFilter = null)
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new RavenDbDocumentHandlerWrapper<TDocument, TEvent>(h)));

    // -------------------------------------------------------------------------
    // TData overloads – TDocument is still a direct type arg of RavenDbProjectionContext
    // in `this`, so inference works the same way as the non-data overloads above.
    // -------------------------------------------------------------------------

    public static ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>
        WhenDocumentExists<TDocument, TEvent, TData>(
            this ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>,
                ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>,
                IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>>? additionalFilter = null)
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new RavenDbDocumentHandlerWrapper<TDocument, TEvent, TData>(h)));

    public static ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>
        WhenDocumentNotExists<TDocument, TEvent, TData>(
            this ISetupHandlerFiltering<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>,
                ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>> configureHandlers,
            Func<IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>,
                IProjectionFilterSetup<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData>>? additionalFilter = null)
        where TDocument : class
        => setup.When(
            f =>
            {
                var filtered = f.WithDocumentFilter(ctx => !ctx.Exists());
                return additionalFilter != null ? additionalFilter(filtered) : filtered;
            },
            h => configureHandlers(new RavenDbDocumentHandlerWrapper<TDocument, TEvent, TData>(h)));
}

/// <summary>
/// Internal wrapper used by <see cref="RavenDbStorageWhenDocumentExtensions.WhenDocumentExists{TDocument,TEvent}"/>
/// and <see cref="RavenDbStorageWhenDocumentExtensions.WhenDocumentNotExists{TDocument,TEvent}"/> to expose
/// the core document marker interfaces over a plain <see cref="ISetupEventHandlerForProjection{TIdContext,TContext,TEvent}"/>.
/// </summary>
internal sealed class RavenDbDocumentHandlerWrapper<TDocument, TEvent>(
    ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent> inner)
    : ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent>,
      ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent>
    where TDocument : class
{
    public ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent> HandleWith(
        Func<TEvent, RavenDbProjectionContext<TDocument>, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);
}

internal sealed class RavenDbDocumentHandlerWrapper<TDocument, TEvent, TData>(
    ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData> inner)
    : ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>,
      ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
    where TDocument : class
{
    public ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent, TData> HandleWith(
        Func<TEvent, RavenDbProjectionContext<TDocument>, TData, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);
}
