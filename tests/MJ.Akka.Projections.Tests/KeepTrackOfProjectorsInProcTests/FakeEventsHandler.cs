using System.Collections.Immutable;
using MJ.Akka.Projections;

namespace MJ.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class FakeEventsHandler : IHandleEventInProjection<object>
{
    public IImmutableList<object> Transform(object evnt)
    {
        return ImmutableList<object>.Empty;
    }

    public DocumentId GetDocumentIdFrom(object evnt)
    {
        return new DocumentId(null, false);
    }

    public Task<(object? document, bool hasHandler)> Handle(
        object? document,
        object evnt,
        long position,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<(object? document, bool hasHandler)>((document, false));
    }
}