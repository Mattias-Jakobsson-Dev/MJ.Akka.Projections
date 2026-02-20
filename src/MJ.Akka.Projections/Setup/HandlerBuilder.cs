namespace MJ.Akka.Projections.Setup;

internal abstract class HandlerBuilder<TId, TContext>
    where TId : notnull where TContext : IProjectionContext
{
    public abstract Handler Build();
    
    public record Handler(
        Func<object, Task<TId>> GetId,
        Func<object, TContext, long, CancellationToken, Task> Handle,
        IProjectionFilter<TContext> Filter);
}

internal class HandlerBuilder<TId, TContext, TEvent>(
    Func<TEvent, Task<TId>> getIdForCurrent,
    IProjectionFilter<TContext> filterForCurrent,
    ISetupProjection<TId, TContext> parent)
    : HandlerBuilder<TId, TContext>, ISetupEventHandlerForProjection<TId, TContext, TEvent>
    where TId : notnull where TContext : IProjectionContext
{
    private readonly List<Func<TEvent, TContext, long?, CancellationToken, Task>>
        _handlers = [];

    public ISetupEventHandlerForProjection<TId, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler)
    {
        _handlers.Add(handler);

        return this;
    }

    public ISetupEventHandlerForProjection<TId, TContext, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, TId> getId, 
        Func<
            IProjectionFilterSetup<TId, TContext, TNewEvent>, 
            IProjectionFilterSetup<TId, TContext, TNewEvent>>? filter = null)
    {
        return parent.On(getId, filter);
    }

    public ISetupEventHandlerForProjection<TId, TContext, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, Task<TId>> getId, 
        Func<
            IProjectionFilterSetup<TId, TContext, TNewEvent>, 
            IProjectionFilterSetup<TId, TContext, TNewEvent>>? filter = null)
    {
        return parent.On(getId, filter);
    }

    IHandleEventInProjection<TId, TContext> ISetupProjectionHandlers<TId, TContext>.Build()
    {
        return parent.Build();
    }

    public override Handler Build()
    {
        return new Handler(
            evnt => getIdForCurrent((TEvent)evnt),
            async (evnt, context, position, cancellationToken) =>
            {
                foreach (var handler in _handlers)
                {
                    await handler(
                        (TEvent)evnt,
                        context, 
                        position,
                        cancellationToken);
                }
            },
            filterForCurrent);
    }
}