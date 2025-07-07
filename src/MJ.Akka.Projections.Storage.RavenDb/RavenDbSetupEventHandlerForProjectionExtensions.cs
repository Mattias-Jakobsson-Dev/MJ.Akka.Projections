using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class RavenDbSetupEventHandlerForProjectionExtensions
{
    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument((evnt, _) => create(evnt));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, long?, TDocument> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument((evnt, position) => Task.FromResult(create(evnt, position)));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument(async (evnt, _, _) => await create(evnt));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        CreateDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, long?, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument(async (evnt, position, _) => await create(evnt, position));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> 
        CreateDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, CancellationToken, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class => setup.CreateDocument(async (evnt, _, cancellationToken) => await create(evnt, cancellationToken));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> 
        CreateDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, long?, CancellationToken, Task<TDocument>> create)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
            context.CreateDocument(await create(evnt, position, cancellationToken)));
    }

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        ModifyDocument<TId, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TDocument?, TDocument> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument((evnt, doc, _) => modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, long?, TDocument> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument((evnt, doc, position) => Task.FromResult(modify(evnt, doc, position)));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> 
        ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, _) => await modify(evnt, doc));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> 
        ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, long?, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, position, _) => await modify(evnt, doc, position));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class => setup.ModifyDocument(async (evnt, doc, _, cancellationToken) => await modify(evnt, doc, cancellationToken));

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> ModifyDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup,
        Func<TEvent, TDocument?, long?, CancellationToken, Task<TDocument>> modify)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith((evnt, context, position, cancellationToken) =>
            context.ModifyDocument(doc => modify(evnt, doc, position, cancellationToken)));
    }

    public static ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent>
        DeleteDocument<TId, TDocument, TEvent>(
        this ISetupEventHandlerForProjection<TId, RavenDbProjectionContext<TDocument>, TEvent> setup)
        where TId : notnull
        where TDocument : class
    {
        return setup.HandleWith((_, context) => context.DeleteDocument());
    }
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, TimeSeriesInput> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, _, _, _) => Task.FromResult(getTimeSeries(evnt)));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, TimeSeriesInput> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, context, _, _) => Task.FromResult(getTimeSeries(evnt, context)));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, TimeSeriesInput> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, context, position, _) => 
        Task.FromResult(getTimeSeries(evnt, context, position)));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, Task<TimeSeriesInput>> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, _, _, _) => getTimeSeries(evnt));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, Task<TimeSeriesInput>> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, context, _, _) => getTimeSeries(evnt, context));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, Task<TimeSeriesInput>> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, context, position, _) => 
        getTimeSeries(evnt, context, position));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent>
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, CancellationToken, Task<TimeSeriesInput>> getTimeSeries)
        where TDocument : class => setup.AddTimeSeries((evnt, context, _, cancellationToken) => 
        getTimeSeries(evnt, context, cancellationToken));
    
    public static ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> 
        AddTimeSeries<TDocument, TEvent>(
            this ISetupEventHandlerForProjection<string, RavenDbProjectionContext<TDocument>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument>, long?, CancellationToken, Task<TimeSeriesInput>> getTimeSeries)
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var timeSeriesInput = await getTimeSeries(evnt, context, position, cancellationToken);

            return
            [
                new StoreTimeSeries(
                    context.Id,
                    timeSeriesInput.Name,
                    ImmutableList.Create(new TimeSeriesRecord(
                        timeSeriesInput.Timestamp,
                        timeSeriesInput.Values,
                        timeSeriesInput.Tag)))
            ];
        });
    }
    
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