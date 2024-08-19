using System.Collections.Immutable;

namespace DC.Akka.Projections;

public interface IHandleEventInProjection<TDocument> where TDocument : notnull
{
    IImmutableList<object> Transform(object evnt);
    DocumentId GetDocumentIdFrom(object evnt);
    Task<(TDocument? document, bool hasHandler)> Handle(TDocument? document, object evnt, long position);
}