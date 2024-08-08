using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_two_simple_documents_with_normal_storage
{
    public class With_string_id(ITestOutputHelper output) : BaseTests<string>(output)
    {
        protected override string FirstDocumentId { get; } = Guid.NewGuid().ToString();
        protected override string SecondDocumentId { get; } = Guid.NewGuid().ToString();
    }
    
    public class With_int_id(ITestOutputHelper output) : BaseTests<int>(output)
    {
        protected override int FirstDocumentId => 1;
        protected override int SecondDocumentId => 2;
    }
    
    public abstract class BaseTests<TId>(ITestOutputHelper output) : BaseProjectionsTest<TId>(output) where TId : notnull
    {
        protected virtual int ExpectedPosition => 2;
        protected abstract TId FirstDocumentId { get; }
        protected abstract TId SecondDocumentId { get; }

        protected readonly string FirstEventId = Guid.NewGuid().ToString();
        protected readonly string SecondEventId = Guid.NewGuid().ToString();

        protected override IImmutableList<object> WhenEvents()
        {
            return ImmutableList.Create<object>(
                new Events<TId>.FirstEvent(FirstDocumentId, FirstEventId),
                new Events<TId>.SecondEvent(SecondDocumentId, SecondEventId));
        }

        [Fact]
        public async Task Then_position_should_be_correct()
        {
            var position = await LoadPosition();

            position.Should().Be(ExpectedPosition);
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
            var doc = await LoadDocument(FirstDocumentId);

            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task Then_second_document_should_be_saved()
        {
            var doc = await LoadDocument(SecondDocumentId);

            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task Then_first_document_should_have_added_one_event()
        {
            var doc = await LoadDocument(FirstDocumentId);

            doc!.HandledEvents.Should().HaveCount(1);
        }

        [Fact]
        public async Task Then_second_document_should_have_added_one_event()
        {
            var doc = await LoadDocument(SecondDocumentId);

            doc!.HandledEvents.Should().HaveCount(1);
        }

        [Fact]
        public async Task Then_first_document_should_have_added_first_event()
        {
            var doc = await LoadDocument(FirstDocumentId);

            doc!.HandledEvents[0].Should().Be(FirstEventId);
        }

        [Fact]
        public async Task Then_second_document_should_have_added_second_event()
        {
            var doc = await LoadDocument(SecondDocumentId);

            doc!.HandledEvents[0].Should().Be(SecondEventId);
        }
    }
}