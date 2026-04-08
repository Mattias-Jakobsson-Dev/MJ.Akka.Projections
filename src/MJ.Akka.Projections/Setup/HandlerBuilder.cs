using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

internal abstract class HandlerBuilder<TIdContext, TContext>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    public abstract Handler Build();
    
    public record Handler(
        Func<object, Task<TIdContext?>> GetId,
        Func<object, TContext, long, CancellationToken, Task> Handle,
        IProjectionFilter<TContext> Filter);
}

internal class HandlerBuilder<TIdContext, TContext, TEvent>(
    Func<TEvent, Task<TIdContext?>> getIdForCurrent,
    IProjectionFilter<TContext> filterForCurrent,
    ISetupProjection<TIdContext, TContext> parent)
    : HandlerBuilder<TIdContext, TContext>, ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    private readonly IProjectionFilter<TContext> _baseFilter = filterForCurrent;

    private readonly List<(Func<TEvent, TContext, long?, CancellationToken, Task> Handler, IProjectionFilter<TContext> Filter)>
        _handlers = [];

    // The filter to apply to the next HandleWith() call. Resets to _baseFilter after each HandleWith().
    private IProjectionFilter<TContext> _pendingFilter = filterForCurrent;

    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> When(
        Func<IProjectionFilterSetup<TIdContext, TContext, TEvent>, IProjectionFilterSetup<TIdContext, TContext, TEvent>> filter)
    {
        var updatedFilter = filter(ProjectionFilterSetup<TIdContext, TContext, TEvent>.Create()).Build();
        _pendingFilter = new CombinedProjectionFilter<TContext>(_baseFilter, updatedFilter);
        return this;
    }

    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler)
    {
        _handlers.Add((handler, _pendingFilter));
        _pendingFilter = _baseFilter;

        return this;
    }

    public ISetupEventHandlerForProjection<TIdContext, TContext, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, TIdContext?> getId)
    {
        return parent.On(getId);
    }

    public ISetupEventHandlerForProjection<TIdContext, TContext, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, Task<TIdContext?>> getId)
    {
        return parent.On(getId);
    }

    IHandleEventInProjection<TIdContext, TContext> ISetupProjectionHandlers<TIdContext, TContext>.Build()
    {
        return parent.Build();
    }

    public override Handler Build()
    {
        return new Handler(
            evnt => getIdForCurrent((TEvent)evnt),
            async (evnt, context, position, cancellationToken) =>
            {
                // Evaluate all filters upfront against the pre-execution context state,
                // so document filters reflect the document as it was loaded — not as
                // mutated by earlier handlers in the same cycle.
                var shouldRun = _handlers
                    .Select(h => h.Filter.FilterEvent(evnt) && h.Filter.FilterResult(context))
                    .ToArray();

                for (var i = 0; i < _handlers.Count; i++)
                {
                    if (!shouldRun[i])
                        continue;

                    await _handlers[i].Handler(
                        (TEvent)evnt,
                        context,
                        position,
                        cancellationToken);
                }
            },
            _baseFilter);
    }
}