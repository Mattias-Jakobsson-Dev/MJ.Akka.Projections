using InfluxDB.Client.Writes;
using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class InfluxDbSetupEventHandlerForProjectionExtensions
{
    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, PointData> getTimeSeries) =>
        setup.AddTimeSeries((evnt, _, _, _) => Task.FromResult(getTimeSeries(evnt)));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, PointData> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, _, _) => Task.FromResult(getTimeSeries(evnt, context)));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, PointData> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, position, _) =>
            Task.FromResult(getTimeSeries(evnt, context, position)));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, Task<PointData>> getTimeSeries) => setup.AddTimeSeries((evnt, _, _, _) => getTimeSeries(evnt));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, Task<PointData>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, _, _) => getTimeSeries(evnt, context));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, Task<PointData>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, position, _) =>
            getTimeSeries(evnt, context, position));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, CancellationToken, Task<PointData>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, _, cancellationToken) =>
            getTimeSeries(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, CancellationToken, Task<PointData>> getTimeSeries)
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        [
            new InfluxDbWritePoint(context.Id, await getTimeSeries(evnt, context, position, cancellationToken))
        ]);
    }
    
    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, DeleteTimeSeriesInput> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, _, _, _) => Task.FromResult(getTimeSeries(evnt)));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, DeleteTimeSeriesInput> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, _, _) => Task.FromResult(getTimeSeries(evnt, context)));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, DeleteTimeSeriesInput> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, position, _) =>
            Task.FromResult(getTimeSeries(evnt, context, position)));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, Task<DeleteTimeSeriesInput>> getTimeSeries) => setup.DeleteTimeSeries((evnt, _, _, _) => getTimeSeries(evnt));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, Task<DeleteTimeSeriesInput>> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, _, _) => getTimeSeries(evnt, context));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, Task<DeleteTimeSeriesInput>> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, position, _) =>
            getTimeSeries(evnt, context, position));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, CancellationToken, Task<DeleteTimeSeriesInput>> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, _, cancellationToken) =>
            getTimeSeries(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<string, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, CancellationToken, Task<DeleteTimeSeriesInput>> getTimeSeries)
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var seriesToDelete = await getTimeSeries(evnt, context, position, cancellationToken);
            
            return
            [
                new InfluxDbDeletePoint(context.Id, seriesToDelete.Start, seriesToDelete.Stop, seriesToDelete.Predicate)
            ];
        });
    }
}