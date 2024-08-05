using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;

namespace DC.Akka.Projections.Tests;

public class When_projecting_transformation_to_two_events_to_simple_document
    : When_projecting_two_events_to_simple_document_with_normal_storage
{
    protected override int ExpectedPosition => 1;

    protected override IImmutableList<object> WhenEvents()
    {
        return ImmutableList.Create<object>(new Events.TransformToMultipleEvents(ImmutableList.Create<Events.IEvent>(
            new Events.FirstEvent("1"),
            new Events.SecondEvent("1"))));
    }
}