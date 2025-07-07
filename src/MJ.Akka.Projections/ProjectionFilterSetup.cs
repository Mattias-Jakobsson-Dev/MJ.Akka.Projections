namespace MJ.Akka.Projections;

internal class ProjectionFilterSetup<TId, TContext, TEvent> : IProjectionFilterSetup<TId, TContext, TEvent>
    where TId : notnull where TContext : IProjectionContext
{
    private readonly Func<TEvent, bool> _eventFilter;
    private readonly Func<TContext, bool> _resultFilter;

    private ProjectionFilterSetup(
        Func<TEvent, bool> eventFilter,
        Func<TContext, bool> resultFilter)
    {
        _eventFilter = eventFilter;
        _resultFilter = resultFilter;
    }

    public static ProjectionFilterSetup<TId, TContext, TEvent> Create()
    {
        return new ProjectionFilterSetup<TId, TContext, TEvent>(
            _ => true,
            _ => true);
    }

    public IProjectionFilterSetup<TId, TContext, TEvent> WithEventFilter(Func<TEvent, bool> filter)
    {
        var previousFilter = _eventFilter;
        
        return new ProjectionFilterSetup<TId, TContext, TEvent>(
            evnt => filter(evnt) && previousFilter(evnt),
            _resultFilter);
    }

    public IProjectionFilterSetup<TId, TContext, TEvent> WithDocumentFilter(Func<TContext, bool> filter)
    {
        var previousFilter = _resultFilter;

        return new ProjectionFilterSetup<TId, TContext, TEvent>(
            _eventFilter,
            document => filter(document) && previousFilter(document));
    }

    public IProjectionFilter<TContext> Build()
    {
        return new ProjectionFilter(_eventFilter, _resultFilter);
    }
    
    private class ProjectionFilter(Func<TEvent, bool> eventFilter, Func<TContext, bool> resultFilter)
        : IProjectionFilter<TContext>
    {
        public bool FilterEvent(object evnt)
        {
            return eventFilter((TEvent)evnt);
        }

        public bool FilterResult(TContext context)
        {
            return resultFilter(context);
        }
    }
}