using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IHandleEventInProjection<TId, in TContext> where TId : notnull where TContext : IProjectionContext
{
    IImmutableList<object> Transform(object evnt);
    
    DocumentId GetDocumentIdFrom(object evnt);
    
    Task<(bool handled, IImmutableList<IProjectionResult> results)> Handle(
        TContext context,
        object evnt,
        long position,
        CancellationToken cancellationToken);
}