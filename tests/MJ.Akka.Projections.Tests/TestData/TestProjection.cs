using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Tests.TestData;

public class TestProjection<TId>(
    IAsyncEnumerable<EventWithPosition> events,
    IImmutableList<StorageFailures> failures,
    string? overrideName = null,
    long? initialPosition = null)
    : TestProjectionWithCustomIdContext<SimpleIdContext<TId>, TId>(
        events,
        failures,
        x => x,
        overrideName,
        initialPosition)
    where TId : notnull
{
    public TestProjection(
        IEnumerable<object> events,
        IImmutableList<StorageFailures> failures,
        string? overrideName = null,
        long? initialPosition = null) : this(
        events.Select((x, i) => new EventWithPosition(x, i + 1)).ToAsyncEnumerable(),
        failures,
        overrideName,
        initialPosition)
    {
    }
}