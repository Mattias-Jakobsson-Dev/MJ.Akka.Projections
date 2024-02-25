namespace DC.Akka.Projections;

public interface IHandleEventInProjection<out TId, TDocument> where TId : notnull where TDocument : notnull
{
    TId? GetDocumentIdFrom(object evnt);
    Task<TDocument?> Handle(TDocument? document, object evnt, long position);
}