using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

internal abstract class HandlerFilteringBuilderBase<TIdContext, TContext>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public record Handler(
        Func<object, TIdContext?> GetId,
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
            evnt => getId((TEvent)evnt),
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

internal class EventRoutingBuilder<TIdContext, TContext, TEvent>(SetupProjection<TIdContext, TContext> parent)
    : ISetupEventRouting<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public ISetupProjection<TIdContext, TContext> Transform(Func<TEvent, IImmutableList<object>> transform)
        => parent.RegisterTransformer(transform);

    public ISetupHandlerFiltering<TIdContext, TContext, TEvent> WithId(Func<TEvent, TIdContext?> getId)
        => parent.GetOrCreateHandlerBuilder(getId);
}