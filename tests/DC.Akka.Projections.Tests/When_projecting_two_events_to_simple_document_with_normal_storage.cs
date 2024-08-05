using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_simple_document_with_normal_storage : BaseProjectionsTest
{
    protected virtual int ExpectedPosition => 2;
    
    protected override IImmutableList<object> WhenEvents()
    {
        return ImmutableList.Create<object>(
            new Events.FirstEvent("1"),
            new Events.SecondEvent("1"));
    }

    [Fact]
    public async Task Then_position_should_be_correct()
    {
        var position = await LoadPosition();

        position.Should().Be(ExpectedPosition);
    }
    
    [Fact]
    public async Task Then_document_should_be_saved()
    {
        var doc = await LoadDocument("1");

        doc.Should().NotBeNull();
    }
    
    [Fact]
    public async Task Exactly_one_document_should_be_saved()
    {
        var docs = await LoadAllDocuments();

        docs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Then_document_should_have_added_two_events()
    {
        var doc = await LoadDocument("1");

        doc!.HandledEvents.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task Then_document_should_have_added_first_event()
    {
        var doc = await LoadDocument("1");

        doc!.HandledEvents[0].Should().BeOfType<Events.FirstEvent>();
    }
    
    [Fact]
    public async Task Then_document_should_have_added_second_event()
    {
        var doc = await LoadDocument("1");

        doc!.HandledEvents[1].Should().BeOfType<Events.SecondEvent>();
    }
}