namespace DC.Akka.Projections;

internal class ProjectionFilterSetup<TDocument, TEvent> : IProjectionFilterSetup<TDocument, TEvent>
    where TDocument : notnull
{
    private readonly Func<TEvent, bool> _eventFilter;
    private readonly Func<TDocument?, bool> _documentFilter;

    private ProjectionFilterSetup(
        Func<TEvent, bool> eventFilter,
        Func<TDocument?, bool> documentFilter)
    {
        _eventFilter = eventFilter;
        _documentFilter = documentFilter;
    }

    public static ProjectionFilterSetup<TDocument, TEvent> Create()
    {
        return new ProjectionFilterSetup<TDocument, TEvent>(
            _ => true,
            _ => true);
    }

    public IProjectionFilterSetup<TDocument, TEvent> WithEventFilter(Func<TEvent, bool> filter)
    {
        var previousFilter = _eventFilter;
        
        return new ProjectionFilterSetup<TDocument, TEvent>(
            evnt => filter(evnt) && previousFilter(evnt),
            _documentFilter);
    }

    public IProjectionFilterSetup<TDocument, TEvent> WithDocumentFilter(Func<TDocument?, bool> filter)
    {
        var previousFilter = _documentFilter;

        return new ProjectionFilterSetup<TDocument, TEvent>(
            _eventFilter,
            document => filter(document) && previousFilter(document));
    }

    public IProjectionFilter<TDocument> Build()
    {
        return new ProjectionFilter(_eventFilter, _documentFilter);
    }
    
    private class ProjectionFilter(Func<TEvent, bool> eventFilter, Func<TDocument?, bool> documentFilter)
        : IProjectionFilter<TDocument>
    {
        public bool FilterEvent(object evnt)
        {
            return eventFilter((TEvent)evnt);
        }

        public bool FilterDocument(TDocument? document)
        {
            return documentFilter(document);
        }
    }
}