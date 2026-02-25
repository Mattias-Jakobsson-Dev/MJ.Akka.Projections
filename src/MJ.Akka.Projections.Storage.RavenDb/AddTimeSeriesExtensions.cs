using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public static class AddTimeSeriesExtensions
{
    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, TimeSeriesInput> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.AddTimeSeries((evnt, _, _, _) => Task.FromResult(getTimeSeries(evnt)));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, TimeSeriesInput> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class =>
        setup.AddTimeSeries((evnt, context, _, _) => Task.FromResult(getTimeSeries(evnt, context)));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, DocumentHandlingMetaData<TIdContext>, TimeSeriesInput> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.AddTimeSeries((evnt, context, metadata, _) =>
        Task.FromResult(getTimeSeries(evnt, context, metadata)));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, Task<TimeSeriesInput>> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.AddTimeSeries((evnt, _, _, _) => getTimeSeries(evnt));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, Task<TimeSeriesInput>> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.AddTimeSeries((evnt, context, _, _) => getTimeSeries(evnt, context));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, DocumentHandlingMetaData<TIdContext>, Task<TimeSeriesInput>> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.AddTimeSeries((evnt, context, metadata, _) =>
        getTimeSeries(evnt, context, metadata));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, CancellationToken, Task<TimeSeriesInput>> getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class => setup.AddTimeSeries((evnt, context, _, cancellationToken) =>
        getTimeSeries(evnt, context, cancellationToken));

    public static ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        AddTimeSeries<TIdContext, TDocument, TEvent>(
            this ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> setup,
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, DocumentHandlingMetaData<TIdContext>, CancellationToken, Task<TimeSeriesInput>>
                getTimeSeries)
        where TIdContext : IProjectionIdContext
        where TDocument : class
    {
        return setup.HandleWith(async (evnt, context, position, cancellationToken) =>
        {
            var timeSeriesInput = await getTimeSeries(
                evnt, 
                context, 
                new DocumentHandlingMetaData<TIdContext>(context.Id, position), 
                cancellationToken);

            context.AddTimeSeries(timeSeriesInput);
        });
    }
}