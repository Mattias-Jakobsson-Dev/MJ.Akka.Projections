using System.Collections.Immutable;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class InfluxDbSetupEventHandlerForProjectionExtensions
{
    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, IEnumerable<PointData>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, _, _, _) => Task.FromResult(getTimeSeries(evnt)));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, IEnumerable<PointData>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, _, _) => Task.FromResult(getTimeSeries(evnt, context)));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, IEnumerable<PointData>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, position, _) =>
            Task.FromResult(getTimeSeries(evnt, context, position)));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, Task<IEnumerable<PointData>>> getTimeSeries) => setup.AddTimeSeries((evnt, _, _, _) => getTimeSeries(evnt));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, Task<IEnumerable<PointData>>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, _, _) => getTimeSeries(evnt, context));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, Task<IEnumerable<PointData>>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, position, _) =>
            getTimeSeries(evnt, context, position));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, CancellationToken, Task<IEnumerable<PointData>>> getTimeSeries) =>
        setup.AddTimeSeries((evnt, context, _, cancellationToken) =>
            getTimeSeries(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        AddTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, CancellationToken, Task<IEnumerable<PointData>>> getTimeSeries)
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var pointsToAdd = await getTimeSeries(evnt, context, position, cancellationToken);
            
            context.AddPoints(pointsToAdd.ToImmutableList());
        });
    }
    
    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, DeleteTimeSeriesInput> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, _, _, _) => Task.FromResult(getTimeSeries(evnt)));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, DeleteTimeSeriesInput> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, _, _) => Task.FromResult(getTimeSeries(evnt, context)));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, DeleteTimeSeriesInput> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, position, _) =>
            Task.FromResult(getTimeSeries(evnt, context, position)));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, Task<DeleteTimeSeriesInput>> getTimeSeries) => setup.DeleteTimeSeries((evnt, _, _, _) => getTimeSeries(evnt));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, Task<DeleteTimeSeriesInput>> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, _, _) => getTimeSeries(evnt, context));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, Task<DeleteTimeSeriesInput>> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, position, _) =>
            getTimeSeries(evnt, context, position));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, CancellationToken, Task<DeleteTimeSeriesInput>> getTimeSeries) =>
        setup.DeleteTimeSeries((evnt, context, _, cancellationToken) =>
            getTimeSeries(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent>
        DeleteTimeSeries<TEvent>(
            this ISetupEventHandlerForProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, TEvent> setup,
            Func<TEvent, InfluxDbTimeSeriesContext, long?, CancellationToken, Task<DeleteTimeSeriesInput>> getTimeSeries)
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var seriesToDelete = await getTimeSeries(evnt, context, position, cancellationToken);
            
            context.DeletePoint(seriesToDelete);
        });
    }
}