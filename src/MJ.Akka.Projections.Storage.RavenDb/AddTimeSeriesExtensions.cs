using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class AddTimeSeriesExtensions
{
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

            context.AddTimeSeries(timeSeriesInput);
        });
    }
}