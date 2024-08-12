using System.Collections.Immutable;

namespace DC.Akka.Projections;

public interface IHandleEventInProjection<TId, TDocument> where TId : notnull where TDocument : notnull
{
    IImmutableList<object> Transform(object evnt);
    DocumentIdResponse GetDocumentIdFrom(object evnt);
    Task<(TDocument? document, bool hasHandler)> Handle(TDocument? document, object evnt, long position);

    public record DocumentIdResponse(TId? Id, bool HasMatch);
}