using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_simple_document_with_normal_storage
{
    public class With_string_id : BaseTests<string>
    {
        protected override string DocumentId { get; } = Guid.NewGuid().ToString();
    }
    
    public class With_int_id : BaseTests<int>
    {
        protected override int DocumentId => 1;
    }
    
    public abstract class BaseTests<TId> : BaseProjectionsTest<TId> where TId : notnull
    {
        protected virtual int ExpectedPosition => 2;
        protected abstract TId DocumentId { get; }

        protected readonly string FirstEventId = Guid.NewGuid().ToString();
        protected readonly string SecondEventId = Guid.NewGuid().ToString();
        
        protected override IImmutableList<object> WhenEvents()
        {
            return ImmutableList.Create<object>(
                new Events<TId>.FirstEvent(DocumentId, FirstEventId),
                new Events<TId>.SecondEvent(DocumentId, SecondEventId));
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
            var doc = await LoadDocument(DocumentId);

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
            var doc = await LoadDocument(DocumentId);

            doc!.HandledEvents.Should().HaveCount(2);
        }
    
        [Fact]
        public async Task Then_document_should_have_added_first_event()
        {
            var doc = await LoadDocument(DocumentId);

            doc!.HandledEvents[0].Should().Be(FirstEventId);
        }
    
        [Fact]
        public async Task Then_document_should_have_added_second_event()
        {
            var doc = await LoadDocument(DocumentId);

            doc!.HandledEvents[1].Should().Be(SecondEventId);
        }
    }
}