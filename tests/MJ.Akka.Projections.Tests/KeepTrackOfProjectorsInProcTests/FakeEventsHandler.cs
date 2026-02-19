using System.Collections.Immutable;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class FakeEventsHandler : IHandleEventInProjection<object, InMemoryProjectionContext<object, object>>
{
    public IImmutableList<object> Transform(object evnt)
    {
        return ImmutableList<object>.Empty;
    }

    public Task<DocumentId> GetDocumentIdFrom(object evnt)
    {
        return Task.FromResult(new DocumentId(null, false));
    }

    public Task<(bool handled, IImmutableList<IProjectionResult> results)> Handle(
        InMemoryProjectionContext<object, object> context, 
        object evnt, 
        long position, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult<(bool handled, IImmutableList<IProjectionResult> results)>(
            (false, ImmutableList<IProjectionResult>.Empty));
    }
}