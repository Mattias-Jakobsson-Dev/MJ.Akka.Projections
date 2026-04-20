using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class FakeEventsHandler : IHandleEventInProjection<SimpleIdContext<object>, InMemoryProjectionContext<object, object>>
{
    public Task<IImmutableList<object>> Transform(object evnt)
    {
        return Task.FromResult<IImmutableList<object>>(ImmutableList<object>.Empty);
    }

    public Task<object> PrepareEvent(object evnt) => Task.FromResult(evnt);

    public SimpleIdContext<object>? GetIdContextFor(object evnt) => null;

    public Task<bool> Handle(
        InMemoryProjectionContext<object, object> context, 
        object evnt, 
        long position, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}