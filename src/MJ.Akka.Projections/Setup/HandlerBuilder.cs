using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

internal abstract class HandlerFilteringBuilderBase<TIdContext, TContext>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public record Handler(
        Func<object, Task<object>> PrepareEvent,
        Func<object, TIdContext?> GetId,
        Func<object, Task<IImmutableList<object>>>? Transform,
        Func<object, TContext, long?, CancellationToken, Task> Handle,
        IProjectionFilter<TContext> Filter);

    public abstract Handler BuildHandler();
}

internal class HandlerFilteringBuilder<TIdContext, TContext, TEvent>(
    Func<TEvent, TIdContext?> getId,
    SetupProjection<TIdContext, TContext> parent)
    : HandlerFilteringBuilderBase<TIdContext, TContext>, ISetupHandlerFiltering<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    private readonly List<(IProjectionFilter<TContext> Filter, List<Func<TEvent, TContext, long?, CancellationToken, Task>> Handlers)> _groups = [];

    public ISetupEventRouting<TIdContext, TContext, TNewEvent> On<TNewEvent>()
        => parent.On<TNewEvent>();

    IHandleEventInProjection<TIdContext, TContext> ISetupProjection<TIdContext, TContext>.Build()
        => parent.Build();

    public ISetupHandlerFiltering<TIdContext, TContext, TEvent> When(
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>> filter,
        Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>> configureHandlers)
    {
        var builtFilter = filter(ProjectionFilterSetup<TIdContext, TContext, TEvent>.Create()).Build();
        var handlerSetup = new EventHandlerSetup<TIdContext, TContext, TEvent>();
        configureHandlers(handlerSetup);
        _groups.Add((builtFilter, handlerSetup.Handlers));
        return this;
    }

    public override Handler BuildHandler()
    {
        var groups = _groups.ToList();
        var passAllFilter = ProjectionFilterSetup<TIdContext, TContext, TEvent>.Create().Build();

        return new Handler(
            Task.FromResult,
            evnt => getId((TEvent)evnt),
            null,
            async (evnt, context, position, cancellationToken) =>
            {
                var shouldRun = groups
                    .Select(g => g.Filter.FilterEvent(evnt) && g.Filter.FilterResult(context))
                    .ToArray();

                for (var i = 0; i < groups.Count; i++)
                {
                    if (!shouldRun[i]) continue;
                    foreach (var handler in groups[i].Handlers)
                        await handler((TEvent)evnt, context, position, cancellationToken);
                }
            },
            passAllFilter);
    }
}

internal class HandlerFilteringBuilderWithData<TIdContext, TContext, TEvent, TData>(
    Func<TEvent, Task<TData>> getData,
    Func<TEvent, TData, TIdContext?> getId,
    SetupProjection<TIdContext, TContext> parent)
    : HandlerFilteringBuilderBase<TIdContext, TContext>, ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    private readonly List<(IProjectionFilter<TContext> Filter, List<Func<TEvent, TContext, TData, long?, CancellationToken, Task>> Handlers)> _groups = [];

    public ISetupEventRouting<TIdContext, TContext, TNewEvent> On<TNewEvent>()
        => parent.On<TNewEvent>();

    IHandleEventInProjection<TIdContext, TContext> ISetupProjection<TIdContext, TContext>.Build()
        => parent.Build();

    public ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> When(
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>, IProjectionFilterSetup<TIdContext, TContext, TEvent, TData>> filter,
        Func<ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>> configureHandlers)
    {
        var builtFilter = filter(ProjectionFilterSetupWithData<TIdContext, TContext, TEvent, TData>.Create()).Build();
        var handlerSetup = new EventHandlerSetupWithData<TIdContext, TContext, TEvent, TData>();
        configureHandlers(handlerSetup);
        _groups.Add((builtFilter, handlerSetup.Handlers));
        return this;
    }

    public override Handler BuildHandler()
    {
        var groups = _groups.ToList();
        var passAllFilter = ProjectionFilterSetupWithData<TIdContext, TContext, TEvent, TData>.Create().Build();

        return new Handler(
            // PrepareEvent: fetch data once, pack it into an envelope that travels with the event
            async rawEvnt =>
            {
                var data = await getData((TEvent)rawEvnt);
                return new EventEnvelope<TData>(rawEvnt, data);
            },
            // GetId: unwrap the envelope and use the data that was already fetched
            evnt =>
            {
                var e = (EventEnvelope<TData>)evnt;
                return getId((TEvent)e.OriginalEvent, e.Data);
            },
            null,
            // Handle: unwrap the envelope; both original event and data are available
            async (evnt, context, position, cancellationToken) =>
            {
                var e = (EventEnvelope<TData>)evnt;

                var shouldRun = groups
                    .Select(g => g.Filter.FilterEvent(e.OriginalEvent) && g.Filter.FilterResult(context))
                    .ToArray();

                for (var i = 0; i < groups.Count; i++)
                {
                    if (!shouldRun[i]) continue;
                    foreach (var handler in groups[i].Handlers)
                        await handler((TEvent)e.OriginalEvent, context, e.Data, position, cancellationToken);
                }
            },
            passAllFilter);
    }
}

internal class EventHandlerSetup<TIdContext, TContext, TEvent>
    : ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public List<Func<TEvent, TContext, long?, CancellationToken, Task>> Handlers { get; } = [];

    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler)
    {
        Handlers.Add(handler);
        return this;
    }
}

internal class EventHandlerSetupWithData<TIdContext, TContext, TEvent, TData>
    : ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public List<Func<TEvent, TContext, TData, long?, CancellationToken, Task>> Handlers { get; } = [];

    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData> HandleWith(
        Func<TEvent, TContext, TData, long?, CancellationToken, Task> handler)
    {
        Handlers.Add(handler);
        return this;
    }
}

internal class EventRoutingBuilder<TIdContext, TContext, TEvent>(SetupProjection<TIdContext, TContext> parent)
    : ISetupEventRouting<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public ISetupProjection<TIdContext, TContext> Transform(Func<TEvent, IImmutableList<object>> transform)
        => parent.RegisterTransformer(transform);

    public ISetupHandlerFiltering<TIdContext, TContext, TEvent> WithId(Func<TEvent, TIdContext?> getId)
        => parent.GetOrCreateHandlerBuilder(getId);

    public ISetupEventRouting<TIdContext, TContext, TEvent, TData> WithData<TData>(Func<TEvent, Task<TData>> getData)
        => new EventRoutingBuilderWithData<TIdContext, TContext, TEvent, TData>(getData, parent);
}

internal class EventRoutingBuilderWithData<TIdContext, TContext, TEvent, TData>(
    Func<TEvent, Task<TData>> getData,
    SetupProjection<TIdContext, TContext> parent)
    : ISetupEventRouting<TIdContext, TContext, TEvent, TData>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public ISetupProjection<TIdContext, TContext> Transform(Func<TEvent, TData, IImmutableList<object>> transform)
        => parent.RegisterTransformerWithData(getData, transform);

    public ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> WithId(Func<TEvent, TData, TIdContext?> getId)
        => parent.GetOrCreateHandlerBuilderWithData(getData, getId);
}

