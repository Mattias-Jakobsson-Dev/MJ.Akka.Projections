using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.RavenDb;

// ReSharper disable once CheckNamespace
namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class RavenDbStorageDeleteDocumentExtensions
{
     public static ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
        ConditionallyDeleteDocument<TDocument, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, bool> condition)
        where TDocument : class
    {
        setup.HandleWith((evnt, ctx, _, _, _) =>
        {
            if (condition(evnt, ctx)) ctx.DeleteDocument();
            return Task.CompletedTask;
        });
        return setup;
    }

    public static ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
        ConditionallyDeleteDocument<TDocument, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, Task<bool>> condition)
        where TDocument : class
    {
        setup.HandleWith(async (evnt, ctx, _, _, _) =>
        {
            if (await condition(evnt, ctx)) ctx.DeleteDocument();
        });
        return setup;
    }

    public static ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
        ConditionallyDeleteDocument<TDocument, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, CancellationToken, Task<bool>> condition)
        where TDocument : class
    {
        setup.HandleWith(async (evnt, ctx, _, _, ct) =>
        {
            if (await condition(evnt, ctx, ct)) ctx.DeleteDocument();
        });
        return setup;
    }

    public static ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
        ConditionallyDeleteDocument<TDocument, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<TEvent, TData, RavenDbProjectionContext<TDocument>, bool> condition)
        where TDocument : class
    {
        setup.HandleWith((evnt, ctx, data, _, _) =>
        {
            if (condition(evnt, data, ctx)) ctx.DeleteDocument();
            return Task.CompletedTask;
        });
        return setup;
    }

    public static ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
        ConditionallyDeleteDocument<TDocument, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<TEvent, TData, RavenDbProjectionContext<TDocument>, Task<bool>> condition)
        where TDocument : class
    {
        setup.HandleWith(async (evnt, ctx, data, _, _) =>
        {
            if (await condition(evnt, data, ctx)) ctx.DeleteDocument();
        });
        return setup;
    }

    public static ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData>
        ConditionallyDeleteDocument<TDocument, TEvent, TData>(
            this ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent, TData> setup,
            Func<TEvent, TData, RavenDbProjectionContext<TDocument>, CancellationToken, Task<bool>> condition)
        where TDocument : class
    {
        setup.HandleWith(async (evnt, ctx, data, _, ct) =>
        {
            if (await condition(evnt, data, ctx, ct)) ctx.DeleteDocument();
        });
        return setup;
    }
}