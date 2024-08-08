using System.Collections.Immutable;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_that_doesnt_match_projection
{
    public class With_string_id(ITestOutputHelper output) : BaseTests<string>(output)
    {
        protected override string DocumentId { get; } = Guid.NewGuid().ToString();
    }
    
    public class With_int_id(ITestOutputHelper output) : BaseTests<int>(output)
    {
        protected override int DocumentId => 1;
    }
    
    public abstract class BaseTests<TId>(ITestOutputHelper output) : BaseProjectionsTest<TId>(output) 
        where TId : notnull
    {
        protected abstract TId DocumentId { get; }
        
        protected override IImmutableList<object> WhenEvents()
        {
            return ImmutableList.Create<object>(
                new Events<TId>.UnHandledEvent(DocumentId),
                new Events<TId>.UnHandledEvent(DocumentId));
        }

        [Fact]
        public async Task Then_no_documents_should_be_saved()
        {
            var docs = await LoadAllDocuments();

            docs.Should().HaveCount(0);
        }
    
        [Fact]
        public async Task Then_position_should_be_correct()
        {
            var position = await LoadPosition();

            position.Should().Be(2);
        }   
    }
}