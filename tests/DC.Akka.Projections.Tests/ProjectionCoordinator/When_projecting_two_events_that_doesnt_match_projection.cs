using System.Collections.Immutable;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionCoordinator;

public class When_projecting_two_events_that_doesnt_match_projection
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
        [Fact]
        public async Task Then_no_documents_should_be_saved()
        {
            var docs = await fixture.LoadAllDocuments();

            docs.Should().HaveCount(0);
        }
    
        [Fact]
        public async Task Then_position_should_be_correct()
        {
            var position = await fixture.LoadPosition(TestProjection<TId>.GetName());

            position.Should().Be(2);
        }
    }

    [PublicAPI]
    public class NormalStorageFixture<TId> : BaseFixture<TId>
        where TId : notnull
    {
        protected override IProjectionConfigurationSetup<TId, TestDocument<TId>> ConfigureProjection(
            IProjectionConfigurationSetup<TId, TestDocument<TId>> setup)
        {
            return setup;
        }
    }

    [PublicAPI]
    public class BatchedStorageFixture<TId> : BaseFixture<TId>
        where TId : notnull
    {
        protected override IProjectionConfigurationSetup<TId, TestDocument<TId>> ConfigureProjection(
            IProjectionConfigurationSetup<TId, TestDocument<TId>> setup)
        {
            return setup
                .WithProjectionStorage(Storage)
                .Batched()
                .Config;
        }
    }

    public abstract class BaseFixture<TId> : ProjectionCoordinatorTestsBase
        where TId : notnull
    {
        private TId DocumentId { get; } = new Fixture().Create<TId>();

        protected override IProjectionsSetup Configure(IProjectionsSetup setup)
        {
            return setup
                .WithTestProjection<TId>(
                    ImmutableList.Create<object>(
                        new Events<TId>.UnHandledEvent(DocumentId),
                        new Events<TId>.UnHandledEvent(DocumentId)),
                    ConfigureProjection);
        }

        protected abstract IProjectionConfigurationSetup<TId, TestDocument<TId>> ConfigureProjection(
            IProjectionConfigurationSetup<TId, TestDocument<TId>> setup);
    }
}