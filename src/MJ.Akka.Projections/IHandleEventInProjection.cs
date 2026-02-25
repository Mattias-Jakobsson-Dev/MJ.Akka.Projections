using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IHandleEventInProjection<TIdContext, in TContext> 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    IImmutableList<object> Transform(object evnt);
    
    Task<TIdContext?> GetIdContextFor(object evnt);
    
    Task<bool> Handle(
        TContext context,
        object evnt,
        long position,
        CancellationToken cancellationToken);
}