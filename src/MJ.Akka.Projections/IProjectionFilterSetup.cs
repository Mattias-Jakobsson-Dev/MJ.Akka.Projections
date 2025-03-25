namespace MJ.Akka.Projections;

public interface IProjectionFilterSetup<TDocument, out TEvent> 
    where TDocument : notnull
{
    IProjectionFilterSetup<TDocument, TEvent> WithEventFilter(Func<TEvent, bool> filter);
    IProjectionFilterSetup<TDocument, TEvent> WithDocumentFilter(Func<TDocument?, bool> filter);

    internal IProjectionFilter<TDocument> Build();
}