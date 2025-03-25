using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IHandleEventInProjection<TDocument> where TDocument : notnull
{
    IImmutableList<object> Transform(object evnt);
    DocumentId GetDocumentIdFrom(object evnt);
    Task<(TDocument? document, bool hasHandler)> Handle(
        TDocument? document,
        object evnt,
        long position,
        CancellationToken cancellationToken);
}