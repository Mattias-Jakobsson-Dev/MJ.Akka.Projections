using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;
using Xunit.Abstractions;

namespace DC.Akka.Projections.Tests;

public class When_projecting_transformation_to_two_events_to_two_simple_documents 
    : When_projecting_two_events_to_two_simple_documents_with_normal_storage
{
    public new class With_string_id(ITestOutputHelper output) : BaseTests<string>(output)
    {
        protected override string FirstDocumentId { get; } = Guid.NewGuid().ToString();
        protected override string SecondDocumentId { get; } = Guid.NewGuid().ToString();
    }
    
    public new class With_int_id(ITestOutputHelper output) : BaseTests<int>(output)
    {
        protected override int FirstDocumentId => 1;
        protected override int SecondDocumentId => 2;
    }

    public new abstract class BaseTests<TId>(ITestOutputHelper output) 
        : When_projecting_two_events_to_two_simple_documents_with_normal_storage.BaseTests<TId>(output) 
        where TId : notnull
    {
        protected override int ExpectedPosition => 1;
    
        protected override IImmutableList<object> WhenEvents()
        {
            return ImmutableList.Create<object>(new Events<TId>.TransformToMultipleEvents(ImmutableList.Create<Events<TId>.IEvent>(
                new Events<TId>.FirstEvent(FirstDocumentId, FirstEventId),
                new Events<TId>.SecondEvent(SecondDocumentId, SecondEventId))));
        }
    }
}