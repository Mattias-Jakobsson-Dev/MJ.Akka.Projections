using System.Collections.Immutable;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionCoordinator;

public class When_projecting_event_that_fails_without_restart_behaviour(
    When_projecting_event_that_fails_without_restart_behaviour.TestFixture fixture)
    : IClassFixture<When_projecting_event_that_fails_without_restart_behaviour.TestFixture>
{
    [Fact]
    public async Task Then_position_should_be_correct()
    {
        var position = await fixture.LoadPosition(TestProjection<string>.GetName());

        position.Should().Be(null);
    }
    
    [Fact]
    public async Task Then_document_should_not_be_saved()
    {
        var doc = await fixture.LoadDocument<TestDocument<string>>(fixture.DocumentId);

        doc.Should().BeNull();
    }
    
    [Fact]
    public async Task Then_no_documents_should_be_saved()
    {
        var docs = await fixture.LoadAllDocuments();

        docs.Should().BeEmpty();
    }

    [Fact]
    public void Then_projection_should_fail()
    {
        fixture
            .GetExceptionFor(TestProjection<string>.GetName())
            .Should()
            .NotBeNull();
    }
    
    [PublicAPI]
    public class TestFixture : ProjectionCoordinatorTestsBase
    {
        public string DocumentId { get; } = new Fixture().Create<string>();
        public string EventId { get; } = Guid.NewGuid().ToString();

        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> setup)
        {
            return setup
                .WithProjectionStreamConfiguration(ProjectionStreamConfiguration.Default with
                {
                    MaxProjectionRetries = 0
                })
                .WithTestProjection<string>(
                    ImmutableList.Create<object>(
                        new Events<string>.FailProjection(
                            DocumentId,
                            EventId,
                            Guid.NewGuid().ToString(),
                            1,
                            new Exception("Projection failed"))));
        }
    }
}