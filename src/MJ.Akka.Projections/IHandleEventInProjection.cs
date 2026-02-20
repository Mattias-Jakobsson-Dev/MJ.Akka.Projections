using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IHandleEventInProjection<TId, in TContext> where TId : notnull where TContext : IProjectionContext
{
    IImmutableList<object> Transform(object evnt);
    
    Task<DocumentId> GetDocumentIdFrom(object evnt);
    
    Task<bool> Handle(
        TContext context,
        object evnt,
        long position,
        CancellationToken cancellationToken);
}