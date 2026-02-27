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
    private readonly List<Func<TEvent, TContext, long?, CancellationToken, Task>>
        _handlers = [];

    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler)
    {
        _handlers.Add(handler);

        return this;
    }

    public ISetupEventHandlerForProjection<TIdContext, TContext, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, Task<TIdContext?>> getId, 
        Func<
            IProjectionFilterSetup<TIdContext, TContext, TNewEvent>, 
            IProjectionFilterSetup<TIdContext, TContext, TNewEvent>>? filter = null)
    {
        return parent.On(getId, filter);
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