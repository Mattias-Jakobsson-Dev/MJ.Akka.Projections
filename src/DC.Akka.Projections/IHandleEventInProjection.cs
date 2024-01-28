namespace DC.Akka.Projections;

public interface IHandleEventInProjection<TDocument>
{
    object? GetDocumentIdFrom(object evnt);
    Task<TDocument?> Handle(TDocument? document, object evnt, long position);
}