using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Tests.TestData;

public class TestProjection<TId>(
    IImmutableList<object> events,
    IImmutableList<StorageFailures> failures,
    string? overrideName = null,
    long? initialPosition = null)
    : TestProjectionWithCustomIdContext<SimpleIdContext<TId>, TId>(
        events,
        failures,
        x => x,
        overrideName, 
        initialPosition)
    where TId : notnull;