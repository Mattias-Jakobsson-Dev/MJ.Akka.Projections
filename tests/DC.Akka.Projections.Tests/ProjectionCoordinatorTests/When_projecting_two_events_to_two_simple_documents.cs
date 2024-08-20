using System.Collections.Immutable;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionCoordinatorTests;

public class When_projecting_two_events_to_two_simple_documents
{
    public class With_normal_storage
    {
        [PublicAPI]
        public class With_string_id(NormalStorageFixture<string> fixture) 
            : BaseTests<string, NormalStorageFixture<string>>(fixture);
        
        [PublicAPI]
        public class With_int_id(NormalStorageFixture<int> fixture) 
            : BaseTests<int, NormalStorageFixture<int>>(fixture);
    }

    public class With_batched_storage
    {
        [PublicAPI]
        public class With_string_id(BatchedStorageFixture<string> fixture) 
            : BaseTests<string, BatchedStorageFixture<string>>(fixture);
        
        [PublicAPI]
        public class With_int_id(BatchedStorageFixture<int> fixture) 
            : BaseTests<int, BatchedStorageFixture<int>>(fixture);
    }

    public abstract class BaseTests<TId, TFixture>(TFixture fixture)
        : IClassFixture<TFixture>
        where TFixture : BaseFixture<TId> where TId : notnull
    {
        protected virtual int ExpectedPosition => 2;

        [Fact]
        public async Task Then_position_should_be_correct()
        {
            var position = await fixture.LoadPosition(TestProjection<TId>.GetName());

            position.Should().Be(ExpectedPosition);
        }

        [Fact]
        public async Task Exactly_two_documents_should_be_saved()
        {
            var docs = await fixture.LoadAllDocuments();

            docs.Should().HaveCount(2);
        }

        [Fact]
        public async Task Then_first_document_should_be_saved()
        {
            var doc = await fixture.LoadDocument<TestDocument<TId>>(fixture.FirstDocumentId);

            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task Then_second_document_should_be_saved()
        {
            var doc = await fixture.LoadDocument<TestDocument<TId>>(fixture.SecondDocumentId);

            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task Then_first_document_should_have_added_one_event()
        {
            var doc = await fixture.LoadDocument<TestDocument<TId>>(fixture.FirstDocumentId);

            doc!.HandledEvents.Should().HaveCount(1);
        }

        [Fact]
        public async Task Then_second_document_should_have_added_one_event()
        {
            var doc = await fixture.LoadDocument<TestDocument<TId>>(fixture.SecondDocumentId);

            doc!.HandledEvents.Should().HaveCount(1);
        }

        [Fact]
        public async Task Then_first_document_should_have_added_first_event()
        {
            var doc = await fixture.LoadDocument<TestDocument<TId>>(fixture.FirstDocumentId);

            doc!.HandledEvents[0].Should().Be(fixture.FirstEventId);
        }

        [Fact]
        public async Task Then_second_document_should_have_added_second_event()
        {
            var doc = await fixture.LoadDocument<TestDocument<TId>>(fixture.SecondDocumentId);

            doc!.HandledEvents[0].Should().Be(fixture.SecondEventId);
        }
        
        [Fact]
        public void Then_projection_should_complete_successfully()
        {
            fixture
                .GetExceptionFor(TestProjection<TId>.GetName())
                .Should()
                .BeNull();
        }
    }

    [PublicAPI]
    public class NormalStorageFixture<TId> : BaseFixture<TId>
        where TId : notnull
    {
        protected override IHaveConfiguration<ProjectionInstanceConfiguration> ConfigureProjection(
            IHaveConfiguration<ProjectionInstanceConfiguration> setup)
        {
            return setup;
        }
    }

    [PublicAPI]
    public class BatchedStorageFixture<TId> : BaseFixture<TId>
        where TId : notnull
    {
        protected override IHaveConfiguration<ProjectionInstanceConfiguration> ConfigureProjection(
            IHaveConfiguration<ProjectionInstanceConfiguration> setup)
        {
            return setup
                .WithProjectionStorage(Storage)
                .Batched();
        }
    }

    public abstract class BaseFixture<TId> : ProjectionCoordinatorTestsBase
        where TId : notnull
    {
        public TId FirstDocumentId { get; } = new Fixture().Create<TId>();
        public TId SecondDocumentId { get; } = new Fixture().Create<TId>();
        public string FirstEventId { get; } = Guid.NewGuid().ToString();
        public string SecondEventId { get; } = Guid.NewGuid().ToString();

        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> setup)
        {
            return setup
                .WithTestProjection<TId>(
                    ImmutableList.Create<object>(
                        new Events<TId>.FirstEvent(FirstDocumentId, FirstEventId),
                        new Events<TId>.SecondEvent(SecondDocumentId, SecondEventId)),
                    ConfigureProjection);
        }

        protected abstract IHaveConfiguration<ProjectionInstanceConfiguration> ConfigureProjection(
            IHaveConfiguration<ProjectionInstanceConfiguration> setup);
    }
}