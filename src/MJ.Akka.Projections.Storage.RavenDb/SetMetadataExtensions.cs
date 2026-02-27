using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class SetMetadataExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, MetadataInput> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, _, _, _) => Task.FromResult(getMetadata(evnt)));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, MetadataInput> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, context, _, _) => Task.FromResult(getMetadata(evnt, context)));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, long?, MetadataInput> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, context, position, _) => 
        Task.FromResult(getMetadata(evnt, context, position)));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, Task<MetadataInput>> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, _, _, _) => getMetadata(evnt));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, Task<MetadataInput>> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, context, _, _) => getMetadata(evnt, context));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, long?, Task<MetadataInput>> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, context, position, _) => 
        getMetadata(evnt, context, position));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, CancellationToken, Task<MetadataInput>> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.SetMetadata((evnt, context, _, cancellationToken) => 
        getMetadata(evnt, context, cancellationToken));
    
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> 
        SetMetadata<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, long?, CancellationToken, Task<MetadataInput>> getMetadata)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var metadataInput = await getMetadata(evnt, context, position, cancellationToken);
            
            context.SetMetadata(metadataInput.Key, metadataInput.Value);
        });
    }
}