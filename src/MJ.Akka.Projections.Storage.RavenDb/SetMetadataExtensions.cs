using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class SetMetadataExtensions
{
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, MetadataInput> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, _, _, _) => Task.FromResult(getMetadata(evnt)));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, MetadataInput> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, context, _, _) => Task.FromResult(getMetadata(evnt, context)));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, MetadataInput> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, context, position, _) => 
        Task.FromResult(getMetadata(evnt, context, position)));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, Task<MetadataInput>> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, _, _, _) => getMetadata(evnt));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, Task<MetadataInput>> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, context, _, _) => getMetadata(evnt, context));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, Task<MetadataInput>> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, context, position, _) => 
        getMetadata(evnt, context, position));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, CancellationToken, Task<MetadataInput>> getMetadata)
        where TDocument : class => setup.SetMetadata((evnt, context, _, cancellationToken) => 
        getMetadata(evnt, context, cancellationToken));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> 
        SetMetadata<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, CancellationToken, Task<MetadataInput>> getMetadata)
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var metadataInput = await getMetadata(evnt, context, position, cancellationToken);

            return
            [
                new StoreMetadata(context.Id, metadataInput.Key, metadataInput.Value)
            ];
        });
    }
}