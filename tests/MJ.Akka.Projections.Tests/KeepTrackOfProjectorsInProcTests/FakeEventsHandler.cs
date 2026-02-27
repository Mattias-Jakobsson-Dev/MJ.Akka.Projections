using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class FakeEventsHandler : IHandleEventInProjection<SimpleIdContext<object>, InMemoryProjectionContext<SimpleIdContext<object>, object>>
{
    public IImmutableList<object> Transform(object evnt)
    {
        return ImmutableList<object>.Empty;
    }

    public Task<SimpleIdContext<object>?> GetIdContextFor(object evnt)
    {
        return Task.FromResult<SimpleIdContext<object>?>(null);
    }

    public Task<bool> Handle(
        InMemoryProjectionContext<SimpleIdContext<object>, object> context, 
        object evnt, 
        long position, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}