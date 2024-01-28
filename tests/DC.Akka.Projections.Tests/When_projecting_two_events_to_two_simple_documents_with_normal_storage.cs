using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_two_simple_documents_with_normal_storage : BaseProjectionsTest
{
    protected override IImmutableList<object> WhenEvents()
    {
        return ImmutableList.Create<object>(
            new Events.FirstEvent("1"),
            new Events.SecondEvent("2"));
    }
    
    [Fact]
    public async Task Then_position_should_be_2()
    {
        var position = await LoadPosition();

        position.Should().Be(2);
    }
    
    [Fact]
    public async Task Exactly_two_documents_should_be_saved()
    {
        var docs = await LoadAllDocuments();

        docs.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task Then_first_document_should_be_saved()
    {
        var doc = await LoadDocument("1");

        doc.Should().NotBeNull();
    }
    
    [Fact]
    public async Task Then_second_document_should_be_saved()
    {
        var doc = await LoadDocument("2");

        doc.Should().NotBeNull();
    }

    [Fact]
    public async Task Then_first_document_should_have_added_one_event()
    {
        var doc = await LoadDocument("1");

        doc!.HandledEvents.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task Then_second_document_should_have_added_one_event()
    {
        var doc = await LoadDocument("2");

        doc!.HandledEvents.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task Then_first_document_should_have_added_first_event()
    {
        var doc = await LoadDocument("1");

        doc!.HandledEvents[0].Should().BeOfType<Events.FirstEvent>();
    }
    
    [Fact]
    public async Task Then_second_document_should_have_added_second_event()
    {
        var doc = await LoadDocument("2");

        doc!.HandledEvents[0].Should().BeOfType<Events.SecondEvent>();
    }
}